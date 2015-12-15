using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using Tango;

/// <summary>
/// Manages points cloud data either from the API, playback file, synthetic room, or test generation.
/// </summary>
public class CreateMeshListener : MonoBehaviour, ITangoDepth
{
    /**
     * Main Camera
     */    
    public Camera m_mainCamera;

    /**
     * Dynamic Mesh Manager
     */    
    public DynamicMeshManager m_meshManager;

    /**
     * Number of points to insert per depth frame update
     */    
    public int m_insertionCount = 1000;

    /**
     * file name of recorded session used for playback
     */    
    public string m_recordingID = "2014_10_26_031617";

    /**
     * Enable Recording
     */    
    private bool m_recordData = false;

    /**
     * Flag for updated depth data
     */        
    private bool m_isDirty;

    /**
     * Copy of the detph data
     */    
    private TangoUnityDepth m_currTangoDepth = new TangoUnityDepth();

    /**
     * Pose of the device when depth data arrived
     */    
    private TangoPoseData m_poseAtDepthTimestamp = new TangoPoseData();

    /**
     * Time stamp used to create unique file recording names
     */        
    private string m_sessionTimestamp = "None";

    /**
     * File writer
     */    
    private BinaryWriter m_fileWriter = null;

    /**
     * File reader
     */    
    private BinaryReader m_fileReader = null;

    /**
     * Index for live preview cubes
     */    
    private int m_cubeIndex = 0;

    /**
     * Motion trail history for depth camera pose
     */    
    private List<Vector3> m_positionHistory = new List<Vector3>();

    /**
     * Initial camera position offset
     */    
    private Vector3 m_initCameraPosition = new Vector3();

    /**
     * Scratch space for raycasting synthetic data
     */    
    private RaycastHit hitInfo = new RaycastHit();

    /**
     * Debug text field
     */    
    private string m_debugText;

    /**
     * Track frame count
     */    
    private int m_frameCount;

    /**
     * Minimum square distance to insert depth data.  
     * Sensor may produce values at 0, should be rejected
     */    
    private float m_sqrMinimumDepthDistance = 0.0625f;

    /**
     * frame of reference for depth data
     */    
    private TangoCoordinateFramePair m_coordinatePair;

    /**
     * Reference to main Tango Application
     */    
    private TangoApplication m_tangoApplication;

    /// <summary>
    /// Use this for initialization.
    /// </summary>
    public void Start() 
    {
        m_isDirty = false;

        m_initCameraPosition = m_mainCamera.transform.position;

        m_coordinatePair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
        m_coordinatePair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE; // FIX should be depth sensor

#if UNITY_ANDROID && !UNITY_EDITOR
        if (m_recordData)
        {
            PrepareRecording();
        }
#endif

        m_tangoApplication = FindObjectOfType<TangoApplication>();
        m_tangoApplication.Register(this);
    }

    /// <summary>
    /// Reset Point Cloud preview, history, and mesh data.
    /// </summary>
    public void Reset()
    {
        m_positionHistory.Clear();
        m_meshManager.Clear();
    }
    
    /// <summary>
    /// Write pose data to file.
    /// </summary>
    /// <param name="writer">File writer.</param>
    /// <param name="pose">Tango pose data.</param>
    public void WritePoseToFile(BinaryWriter writer, TangoPoseData pose)
    {
        if (writer == null)
        {
            return;
        }
        
        writer.Write("poseframe\n");
        writer.Write(pose.timestamp + "\n");
        writer.Write((int)pose.framePair.baseFrame);
        writer.Write((int)pose.framePair.targetFrame);
        writer.Write((int)pose.status_code);
        writer.Write(pose.translation[0]);
        writer.Write(pose.translation[1]);
        writer.Write(pose.translation[2]);
        writer.Write(pose.orientation[0]);
        writer.Write(pose.orientation[1]);
        writer.Write(pose.orientation[2]);
        writer.Write(pose.orientation[3]);
        writer.Flush();
    }

    /// <summary>
    /// Write depth data to file.
    /// </summary>
    /// <param name="writer">File writer.</param>
    /// <param name="depthFrame">Tango depth data.</param>
    public void WriteDepthToFile(BinaryWriter writer, TangoUnityDepth depthFrame)
    {
        if (writer == null)
        {
            return;
        }
        
        writer.Write("depthframe\n");
        writer.Write(depthFrame.m_timestamp + "\n");
        writer.Write(depthFrame.m_pointCount + "\n");
        
        for (int i = 0; i < depthFrame.m_pointCount; i++)
        {
            writer.Write(depthFrame.m_points[(3 * i) + 0]);
            writer.Write(depthFrame.m_points[(3 * i) + 1]);
            writer.Write(depthFrame.m_points[(3 * i) + 2]);
        }
        writer.Flush();
    }

    /// <summary>
    /// An event notifying when new depth data is available. OnTangoDepthAvailable events are thread safe.
    /// </summary>
    /// <param name="tangoDepth">Depth data that we get from API.</param>
    public void OnTangoDepthAvailable(TangoUnityDepth tangoDepth)
    {
        // Fill in the data to draw the point cloud.
        if (tangoDepth != null)
        {
            if (tangoDepth.m_points == null) 
            {
                Debug.Log("Depth points are null");
                return;
            }
            
            if (tangoDepth.m_pointCount > m_currTangoDepth.m_points.Length)
            {
                m_currTangoDepth.m_points = new float[3 * (int)(1.5f * tangoDepth.m_pointCount)];
            }
            
            for (int i = 0; i < tangoDepth.m_pointCount; i += 3)
            {
                m_currTangoDepth.m_points[(3 * i) + 0] = tangoDepth.m_points[(i * 3) + 0];
                m_currTangoDepth.m_points[(3 * i) + 1] = tangoDepth.m_points[(i * 3) + 1];
                m_currTangoDepth.m_points[(3 * i) + 2] = tangoDepth.m_points[(i * 3) + 2];
            }
            m_currTangoDepth.m_timestamp = tangoDepth.m_timestamp;
            m_currTangoDepth.m_pointCount = tangoDepth.m_pointCount;
            
            PoseProvider.GetPoseAtTime(m_poseAtDepthTimestamp, m_currTangoDepth.m_timestamp, m_coordinatePair);
            
            if (m_poseAtDepthTimestamp.status_code != TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
            {
                return;
            }
            
            m_isDirty = true;
        }
        return;
    }

    /// <summary>
    /// Create file for recording.
    /// </summary>
    private void PrepareRecording()
    {
        m_sessionTimestamp = DateTime.Now.ToString("yyyy_MM_dd_HHmmss");
        string filename = m_sessionTimestamp + ".dat";
        if (m_fileWriter != null)
        {
            m_fileWriter.Close();
            m_fileWriter = null;
        }
        m_fileWriter = new BinaryWriter(File.Open(Application.persistentDataPath + "/" + filename, FileMode.Create));
        m_debugText = "Saving to: " + filename + " " + m_fileWriter.ToString();
    }

    /// <summary>
    /// Draw debug frustum lines.
    /// </summary>
    private void DrawDebugLines()
    {
        float frustumSize = 2;
        Color frustumColor = Color.red;
        Debug.DrawLine(transform.position, transform.position + (frustumSize * transform.forward) + transform.right + transform.up, frustumColor);
        Debug.DrawLine(transform.position, transform.position + (frustumSize * transform.forward) - transform.right + transform.up, frustumColor);
        Debug.DrawLine(transform.position, transform.position + (frustumSize * transform.forward) - transform.right - transform.up, frustumColor);
        Debug.DrawLine(transform.position, transform.position + (frustumSize * transform.forward) + transform.right - transform.up, frustumColor);
        Debug.DrawLine(transform.position + (frustumSize * transform.forward) + transform.right + transform.up, transform.position + (frustumSize * transform.forward) + transform.right - transform.up, frustumColor);
        Debug.DrawLine(transform.position + (frustumSize * transform.forward) + transform.right + transform.up, transform.position + (frustumSize * transform.forward) - transform.right + transform.up, frustumColor);
        Debug.DrawLine(transform.position + (frustumSize * transform.forward) - transform.right - transform.up, transform.position + (frustumSize * transform.forward) - transform.right + transform.up, frustumColor);
        Debug.DrawLine(transform.position + (frustumSize * transform.forward) - transform.right - transform.up, transform.position + (frustumSize * transform.forward) + transform.right - transform.up, frustumColor);
    }

    /// <summary>
    /// Update is called once per frame
    /// It processes input keyfor pausing, steping.  It will also playback/record data or generate synthetic data.
    /// If running on Tango device it will process depth data that was copied for the depth callback.
    /// </summary>
    private void Update() 
    {
        m_frameCount++;

        // history
        for (int i = 0; i < m_positionHistory.Count - 1; i++)
        {
            Debug.DrawLine(m_positionHistory[i], m_positionHistory[i + 1], Color.white);
        }

        if (m_recordData) 
        {
            WritePoseToFile(m_fileWriter, m_poseAtDepthTimestamp);
            WriteDepthToFile(m_fileWriter, m_currTangoDepth);
            m_debugText = "Recording Session: " + m_sessionTimestamp + " Points: " + m_currTangoDepth.m_timestamp;
        }

		if (m_isDirty)
        {
            SetTransformUsingTangoPose(transform, m_poseAtDepthTimestamp);

            m_positionHistory.Add(transform.position);

            DrawDebugLines();

            float insertionStartTime = Time.realtimeSinceStartup;

            for (int i = 0; i < m_insertionCount; i++)
            {
                if (i > m_currTangoDepth.m_pointCount)
                {
                    break;
                }

                // randomly sub sample
                int index = i;

                // need to be more graceful than this, does not behave continuously
                if (m_insertionCount < m_currTangoDepth.m_pointCount)
                {
                    index = UnityEngine.Random.Range(0, m_currTangoDepth.m_pointCount);
                }

                Vector3 p = new Vector3(m_currTangoDepth.m_points[3 * index], -m_currTangoDepth.m_points[(3 * index) + 1], m_currTangoDepth.m_points[(3 * index) + 2]);
                float sqrmag = p.sqrMagnitude;

                if (sqrmag < m_sqrMinimumDepthDistance)
                {
                    continue;
                }

                Vector3 tp = transform.TransformPoint(p);

                // less weight for things farther away, because of noise
                if (m_recordData)
                {
					m_meshManager.InsertPoint(tp, transform.forward, 1.0f / (sqrmag + 1.0f));
				}
            }

            float insertionStopTime = Time.realtimeSinceStartup;
            m_meshManager.InsertionTime = (m_meshManager.InsertionTime * m_meshManager.TimeSmoothing) + ((1.0f - m_meshManager.TimeSmoothing) * (insertionStopTime - insertionStartTime));
            m_meshManager.QueueDirtyMeshesForRegeneration();

            m_isDirty = false;
        }
    }

    /// <summary>
    /// Tranform Tango pose data to Unity transform.
    /// </summary>
    /// <param name="xform">Unity Transform output.</param>
    /// <param name="pose">Tango Pose Data.</param>
    private void SetTransformUsingTangoPose(Transform xform, TangoPoseData pose)
    {
        xform.position = new Vector3((float)pose.translation[0],
                                     (float)pose.translation[2],
                                     (float)pose.translation[1]) + m_initCameraPosition;
        
        Quaternion quat = new Quaternion((float)pose.orientation[0],
                                         (float)pose.orientation[2], // these rotation values are swapped on purpose
                                         (float)pose.orientation[1],
                                         (float)pose.orientation[3]);

        Quaternion axisFixedQuat = Quaternion.Euler(-quat.eulerAngles.x,
                                              -quat.eulerAngles.z,
                                              quat.eulerAngles.y);

        // FIX - should query API for depth camera extrinsics
        Quaternion extrinsics = Quaternion.Euler(-12.0f, 0, 0);

        xform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f) * axisFixedQuat * extrinsics;
    }

    /// <summary>
    /// Display some on screen debug infromation and handles the record button.
    /// </summary>
    private void OnGUI()
    {
        //GUI.Label(new Rect(10, 180, 1000, 30), "Depth Points: " + m_currTangoDepth.m_pointCount);
        //GUI.Label(new Rect(10, 200, 1000, 30), "Debug: " + m_debugText);
        if (!m_recordData)
        {
            if (GUI.Button(new Rect(Screen.width - 320, 20, 300, 150), "Start Record"))
            {
                m_meshManager.Clear();
                PrepareRecording();
                m_recordData = true;
            }
        } 
        else
        {
            if (GUI.Button(new Rect(Screen.width - 320, 20, 300, 150), "Stop Record"))
            {
                m_recordData = false;
                m_fileWriter.Close();
                m_fileWriter = null;
                m_debugText = "Stopped Recording";

				Reset();

				Application.LoadLevel("StartScene");
            }
        }
    }
}

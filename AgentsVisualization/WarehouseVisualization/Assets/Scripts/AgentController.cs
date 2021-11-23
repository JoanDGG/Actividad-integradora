﻿// TC2008B. Sistemas Multiagentes y Gráficas Computacionales
// C# client to interact with Python. Based on the code provided by Sergio Ruiz.
// Octavio Navarro. October 2021

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/*
public class CarData
{
    public int uniqueID;
    public Vector3 position;
}
*/
public class ObstacleData
{
    public float x, y, z;
    public string tag;
}
public class AgentData
{
    public float x, y, z;
    public bool robotHasBox;
}

public class AgentController : MonoBehaviour
{
    // private string url = "https://boids.us-south.cf.appdomain.cloud/";
    string serverUrl = "http://localhost:8585";
    string getAgentsEndpoint = "/getRobotAgents";
    string getObstaclesEndpoint = "/getObstacles";
    string getDroppedBoxesEndpoint = "/getDroppedBoxes";
    string sendConfigEndpoint = "/init";
    string updateEndpoint = "/update";
    List<AgentData> robotsData;
    List<ObstacleData> obstacleData;
    GameObject[] agents;
    List<Vector3> oldPositions;
    List<Vector3> newPositions;
    // Pause the simulation while we get the update from the server
    bool hold = false;

    public GameObject robotPrefab, boxPrefab, shelfPrefab, floor, wallPrefab, doorPrefab;
    public int NAgents, NBoxes, width, height, maxShelves;
    public float timeToUpdate = 5.0f, timer, dt;

    void Start()
    {
        robotsData = new List<AgentData>();
        obstacleData = new List<ObstacleData>();
        oldPositions = new List<Vector3>();
        newPositions = new List<Vector3>();

        agents = new GameObject[NAgents];

        floor.transform.localScale = new Vector3((float)width/10, 1, (float)height/10);
        floor.transform.localPosition = new Vector3((float)width/2-0.5f, 0, (float)height/2-0.5f);
        
        timer = timeToUpdate;

        for(int i = 0; i < NAgents; i++)
            agents[i] = Instantiate(robotPrefab, Vector3.zero, Quaternion.identity);
            
        StartCoroutine(SendConfiguration());
    }

    private void Update() 
    {
        float t = timer/timeToUpdate;
        // Smooth out the transition at start and end
        dt = t * t * ( 3f - 2f*t);

        if(timer >= timeToUpdate)
        {
            timer = 0;
            hold = true;
            StartCoroutine(UpdateSimulation());
        }

        if (!hold)
        {
            for (int s = 0; s < agents.Length; s++)
            {
                Vector3 interpolated = Vector3.Lerp(oldPositions[s], newPositions[s], dt);
                agents[s].transform.localPosition = interpolated;
                
                Vector3 dir = oldPositions[s] - newPositions[s];
                agents[s].transform.rotation = Quaternion.LookRotation(dir);
                
            }
            // Move time from the last frame
            timer += Time.deltaTime;
        }
    }
 
    IEnumerator UpdateSimulation()
    {
        UnityWebRequest www = UnityWebRequest.Get(serverUrl + updateEndpoint);
        yield return www.SendWebRequest();
 
        if (www.result != UnityWebRequest.Result.Success)
            Debug.Log(www.error);
        else 
        {
            StartCoroutine(GetRobotsData());
        }
    }

    IEnumerator SendConfiguration()
    {
        WWWForm form = new WWWForm();

        form.AddField("NAgents", NAgents.ToString());
        form.AddField("NBoxes", NBoxes.ToString());
        form.AddField("width", width.ToString());
        form.AddField("height", height.ToString());
        form.AddField("maxShelves", maxShelves.ToString());

        UnityWebRequest www = UnityWebRequest.Post(serverUrl + sendConfigEndpoint, form);
        www.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("Configuration upload complete!");
            Debug.Log("Getting Agents positions");
            StartCoroutine(GetRobotsData());
            StartCoroutine(GetObstacleData());
        }
    }

    IEnumerator GetRobotsData() 
    {
        UnityWebRequest www = UnityWebRequest.Get(serverUrl + getAgentsEndpoint);
        yield return www.SendWebRequest();
 
        if (www.result != UnityWebRequest.Result.Success)
            Debug.Log(www.error);
        else 
        {
            List<bool> currentRobotHasBoxes = new List<bool>();
            robotsData = JsonUtility.FromJson<List<AgentData>>(www.downloadHandler.text);

            // Store the old positions for each agent
            oldPositions = new List<Vector3>(newPositions);

            newPositions.Clear();
            // REVISAR DESDE AQUI
            foreach(AgentData agent in robotsData) {
                newPositions.Add(new Vector3(agent.x, agent.y, agent.z));
                currentRobotHasBoxes.Add(agent.robotHasBox);
            }   

            hold = false;
        }
    }

    IEnumerator GetObstacleData() 
    {
        UnityWebRequest www = UnityWebRequest.Get(serverUrl + getObstaclesEndpoint);
        yield return www.SendWebRequest();
 
        if (www.result != UnityWebRequest.Result.Success)
            Debug.Log(www.error);
        else 
        {
            obstacleData = JsonUtility.FromJson<List<ObstacleData>>(www.downloadHandler.text);
            // Recieve tags from json and check for instantiation
            Debug.Log(obstacleData);

            foreach(ObstacleData obstacle in obstacleData)
            {
                if (obstacle.tag == "box") {
                    Instantiate(boxPrefab, new Vector3(obstacle.x, obstacle.y, obstacle.z), Quaternion.identity);
                }
                else if (obstacle.tag == "shelf") {
                    Instantiate(shelfPrefab, new Vector3(obstacle.x, obstacle.y, obstacle.z), Quaternion.identity);
                }
                else if (obstacle.tag == "border") {
                    Instantiate(wallPrefab, new Vector3(obstacle.x, obstacle.y, obstacle.z), Quaternion.identity);
                }
                
            }
        }
    }
}

// TC2008B. Sistemas Multiagentes y Gráficas Computacionales
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
[System.Serializable]
public class Drop_zone
{
    public float x, y;
}

[System.Serializable]
public class ObstacleData
{
    public float x, y, z;
    public string tag;
    public bool picked_up;
}
[System.Serializable]
public class AgentData
{
    public float x, y, z;
    public bool has_box;
    public int unique_id;
}

[System.Serializable]
public class AgentsData
{
    public List<AgentData> robots_attributes;
}

[System.Serializable]
public class ObstaclesData
{
    public List<ObstacleData> obstacles_attributes;
}

[System.Serializable]
public class Drop_zones
{
    public List<Drop_zone> drop_zone_pos;
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
    AgentsData robotsData;
    ObstaclesData obstacleData;
    GameObject[] agents;
    List<Vector3> oldPositions;
    List<Vector3> newPositions;
    List<int> unique_ids;
    // Pause the simulation while we get the update from the server
    bool hold = false;

    public GameObject robotPrefab, boxPrefab, shelfPrefab, floor, wallPrefab, doorPrefab, drop_zone;
    public int NAgents, NBoxes, width, height, maxShelves;
    public float timeToUpdate = 5.0f, timer, dt;

    void Start()
    {
        robotsData = new AgentsData();
        obstacleData = new ObstaclesData();
        oldPositions = new List<Vector3>();
        newPositions = new List<Vector3>();
        //unique_ids = new List<int>();

        agents = new GameObject[NAgents];

        floor.transform.localScale = new Vector3((float)width/10, 1, (float)height/10);
        floor.transform.localPosition = new Vector3((float)width/2-0.5f, 0, (float)height/2-0.5f);
        
        timer = timeToUpdate;

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
            StartCoroutine(UpdateRobotsData());
            StartCoroutine(UpdateObstaclesData());
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
            //Debug.Log("Configuration upload complete!");
            //Debug.Log("Getting Agents positions");
            StartCoroutine(GetRobotsData());
            StartCoroutine(GetObstacleData());
        }

        www = UnityWebRequest.Get(serverUrl + sendConfigEndpoint);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            // Asignar posicion a drop zone
            Debug.Log(www.downloadHandler.text);
            Drop_zones drop_zones = JsonUtility.FromJson<Drop_zones>(www.downloadHandler.text);
            Debug.Log(drop_zones.drop_zone_pos[0].x + ", " + drop_zones.drop_zone_pos[0].y);
            drop_zone.transform.position = new Vector3(drop_zones.drop_zone_pos[0].x, 2f, drop_zones.drop_zone_pos[0].y);
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
            robotsData = JsonUtility.FromJson<AgentsData>(www.downloadHandler.text);
            // Store the old positions for each agent
            // REVISAR DESDE AQUI
            for (int index_agent = 0; index_agent < robotsData.robots_attributes.Count; index_agent++)
            {
                AgentData agent = robotsData.robots_attributes[index_agent];
                Vector3 agentPosition = new Vector3(agent.x, agent.y, agent.z);
                newPositions.Add(agentPosition);
                agents[index_agent] = Instantiate(robotPrefab, agentPosition, Quaternion.identity);
                //unique_ids.Add(agent.unique_id);
            }
            //print(newPositions.Count);
            //print(currentRobotHasBoxes.Count);

            hold = false;
        }
    }

    IEnumerator UpdateRobotsData() 
    {
        UnityWebRequest www = UnityWebRequest.Get(serverUrl + getAgentsEndpoint);
        yield return www.SendWebRequest();
 
        if (www.result != UnityWebRequest.Result.Success)
            Debug.Log(www.error);
        else 
        {
            robotsData = JsonUtility.FromJson<AgentsData>(www.downloadHandler.text);
            // Store the old positions for each agent
            oldPositions = new List<Vector3>(newPositions);
            newPositions.Clear();


            for(int i = 0; i < robotsData.robots_attributes.Count; i++) {
                AgentData agentData = robotsData.robots_attributes[i];
                newPositions.Add(new Vector3(agentData.x, agentData.y, agentData.z));
                agents[i].transform.GetChild(1).gameObject.SetActive(agentData.has_box);
            }

            /*
            for (int index_agent = 0; index_agent < robotsData.robots_attributes.Count; index_agent++)
            {
                AgentData agent = robotsData.robots_attributes[index_agent];
                newPositions[index_agent] = new Vector3(agent.x, agent.y, agent.z);
                agents[index_agent].transform.GetChild(1).gameObject.SetActive(agent.has_box);
                
                foreach(GameObject a in GameObject.FindGameObjectsWithTag("Robot"))
                {
                    Debug.Log("Coordenadas de agent: " + agent.x + ", " + agent.z);
                    Debug.Log("Coordenadas de a: " + a.transform.position.x + ", " + a.transform.position.z);
                    if (agent.x == a.transform.position.x && 
                    agent.z == a.transform.position.z) {
                        newPositions[index_agent] = new Vector3(agent.x, agent.y, agent.z);
                        //Debug.Log("aaaaaaaaaaaaaaaaaaa" + newPositions[index_agent]);
                        agents[index_agent].transform.GetChild(1).gameObject.SetActive(agent.has_box);
                    }
                }
                
            }*/
            //print(newPositions.Count);
            //print(currentRobotHasBoxes.Count);

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
            obstacleData = JsonUtility.FromJson<ObstaclesData>(www.downloadHandler.text);
            print("Count de obstacles: " + obstacleData.obstacles_attributes.Count);
            // Recieve tags from json and check for instantiation
            
            foreach(ObstacleData obstacle in obstacleData.obstacles_attributes)
            {
                //print("Tag: " + obstacle.tag);
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

    IEnumerator UpdateObstaclesData()
    {
        UnityWebRequest www = UnityWebRequest.Get(serverUrl + getObstaclesEndpoint);
        yield return www.SendWebRequest();
 
        if (www.result != UnityWebRequest.Result.Success)
            Debug.Log(www.error);
        else 
        {
            obstacleData = JsonUtility.FromJson<ObstaclesData>(www.downloadHandler.text);
            // Recieve tags from json and check for instantiation
            
            foreach(ObstacleData obstacle in obstacleData.obstacles_attributes)
            {
                foreach(GameObject box in GameObject.FindGameObjectsWithTag("Box"))
                {
                    //Debug.Log("python obstacle position " + obstacle.x + obstacle.y + obstacle.z);
                    //Debug.Log("Unity obstacle position " + box.transform.position.x + box.transform.position.y + box.transform.position.z);
                    if (obstacle.picked_up && 
                    obstacle.x == box.transform.position.x && 
                    obstacle.z == box.transform.position.z) {
                        Destroy(box);
                    }
                }
            }
        }
    }
}

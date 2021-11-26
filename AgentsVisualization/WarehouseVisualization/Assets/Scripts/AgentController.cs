// TC2008B. Sistemas Multiagentes y Gráficas Computacionales
// C# client to interact with Python. Based on the code provided by Sergio Ruiz.
// Octavio Navarro. October 2021

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

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
    public int unique_id;
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

[System.Serializable]
public class ModelData
{
    public int currentStep;
    public int droppedBoxes;
}
[System.Serializable]
public class DroppedBoxes
{
    public int droppedBoxes;
}

public class AgentController : MonoBehaviour
{
    // private string url = "https://boids.us-south.cf.appdomain.cloud/";
    string serverUrl = "http://localhost:8585";
    string getAgentsEndpoint = "/getRobotAgents";
    string getObstaclesEndpoint = "/getObstacles";
    string sendConfigEndpoint = "/init";
    string updateEndpoint = "/update";
    AgentsData robotsData;
    ObstaclesData obstacleData;
    GameObject[] agents;
    List<Vector3> oldPositions;
    List<Vector3> newPositions;
    // List<int> unique_ids;
    bool hold = false;

    public GameObject robotPrefab, boxPrefab, shelfPrefab, floor, wallPrefab, doorPrefab, drop_zone;
    public Text currentStep;
    public int NAgents, NBoxes, width, height, maxShelves, maxSteps;
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
                if(dir != new Vector3(0, 0, 0))
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
            ModelData model = JsonUtility.FromJson<ModelData>(www.downloadHandler.text);
            currentStep.text = "Boxes found: " + model.droppedBoxes + "/" + NBoxes + "\n" +
                               "Step " + model.currentStep;
            if(model.currentStep >= maxSteps || model.droppedBoxes >= NBoxes)
            {
                currentStep.text += "\nSimulation complete.";
            }
            else
            {
                StartCoroutine(UpdateRobotsData());
                StartCoroutine(UpdateObstaclesData());
            }
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
        form.AddField("maxSteps", maxSteps.ToString());

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
            // Assign position to the drop zone
            Drop_zones drop_zones = JsonUtility.FromJson<Drop_zones>(www.downloadHandler.text);
            drop_zone.transform.position = new Vector3(drop_zones.drop_zone_pos[0].x,
                                                       drop_zone.transform.position.y, 
                                                       drop_zones.drop_zone_pos[0].y);
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
            for (int index_agent = 0; index_agent < robotsData.robots_attributes.Count; index_agent++)
            {
                AgentData agent = robotsData.robots_attributes[index_agent];
                Vector3 agentPosition = new Vector3(agent.x, agent.y, agent.z);
                newPositions.Add(agentPosition);
                agents[index_agent] = Instantiate(robotPrefab, agentPosition, Quaternion.identity);
                //unique_ids.Add(agent.unique_id);
            }

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
            // Recieve tags from json and check for instantiation
            foreach(ObstacleData obstacle in obstacleData.obstacles_attributes)
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
            bool boxGOSurvives;

            foreach(GameObject boxGameObject in GameObject.FindGameObjectsWithTag("Box")) {
                boxGOSurvives = false;
                foreach(ObstacleData obstacle in obstacleData.obstacles_attributes)
                {
                    if (obstacle.tag == "box" && 
                        boxGameObject.transform.position.x == obstacle.x && 
                        boxGameObject.transform.position.z == obstacle.z) 
                    {
                        boxGOSurvives = true;
                    }
                }  
                if (!boxGOSurvives) {
                    Destroy(boxGameObject);
                }     
            }
        }
    }
}

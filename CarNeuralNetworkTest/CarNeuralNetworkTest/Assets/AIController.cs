using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Xml.Serialization;
using System.Threading;

namespace AssemblyCSharp
{
    public enum FitnessMeasure
    {
        distance,
        distance2byTime,
        distanceByTime,
        speed,
        streetParts,
        points
    }

    public class AIController : MonoBehaviour
    {
        public bool training = true;

        public bool LoadLastNerual = true;
        public bool newNetworkVersion = false;
        public FitnessMeasure fitnessMeasure;
        public int NetworkUpdateSpeed = 1;
        public UnityEngine.Object child;
        public bool spawnChild = false;
        public bool childRespawnAtParent = true;
        public bool ignoreFirstCollide = false;
        public int timeScale;

        public float maxSpeed = 1.5f;
        public int population = 20;
        public float timeLimit = 20;

        public float frontForce, backForce, leftForce, rightForce;

        public int mutationRateStatic;
        public int mutationRate;
        public int generation = 0;
        public double[] points;
        public double[] results;
        public double[] sensors;

        [Header("Sensor Settings")]
        public float sensoreLength = 5f;
        public float frontSensorPosition = 0f;
        public float frontSideSensorPosition = 0.2f;
        public float frontSesnorAngle = 0.5f;
        public float senoreHight = 0f;


        [Header("Read only")]
        public NeuralChildObject stats;

        public float bestDistance = 0;
        public Network bestNetwork;
        bool saveBestNetwork = false;
        public double bestScore;
        [SerializeField] public NeuralChildObject bestStats;
        public float time = 0f;
        public float speed = 0f;
        int id = 0;
        bool ignoreCollide = false;
        public int childs = 0;
        public int deadChilds = 0;
        GameObject[] gameObjectsChilds;
        
        //layers (input, hidden, output)
        public int[] parameters = { 10,16,32,16,6};

        Network[] networks;
        Network lastBest = null;
        NeuralNetwork.NetworkModels.Network newNetworks;
        RaycastHit hit;

        int networkUpdateStatus = 0;
        Network[] networkUpdateParameter;

        Quaternion startRotation;
        Vector3 startPosition;

        GameObject childFolder;
       

        void mutationUpdate()
        {
            if (mutationRateStatic != mutationRate)
            {
                mutationRateStatic = mutationRate;
                if (newNetworkVersion)
                {
                    newNetworks.LearnRate = mutationRate;
                }
                Debug.Log("[" + transform.root.gameObject.name + "] Mutation updated!");
            }
        }

        // Use this for initialization
        void Start()
        {
            childFolder = new GameObject();
            childFolder.name = "[" + transform.root.gameObject.name + "] Childs";
            stats = new NeuralChildObject();
            InvokeRepeating("mutationUpdate", 1, 1);
            startPosition = transform.position;
            startRotation = transform.rotation;
            stats.LastPosition = startPosition;

            ignoreCollide = ignoreFirstCollide;

            Debug.Log("[" + transform.root.gameObject.name + "] Generation " + generation);

            results = new double[4];
            points = new double[population];
            sensors = new double[10];

            networks = new Network[population];
            newNetworks = new NeuralNetwork.NetworkModels.Network(parameters[0], new int[] { 8, 8, 8 }, 4, 2,1);
            gameObjectsChilds = new GameObject[population];
            gameObjectsChilds[0] = this.gameObject;
            NeuralWeights loadedWeightsValues = null;
            if (LoadLastNerual)
            {
                string m_Path = Application.dataPath + "/" + transform.root.gameObject.name + ".xml";
                print(m_Path);
                loadedWeightsValues = Load(m_Path);
                if (loadedWeightsValues != null)
                {
                    bestScore = loadedWeightsValues.bestScore;
                    generation = loadedWeightsValues.Generation;
                    print("[" + transform.root.gameObject.name + "] LoadedWeights");
                }
            }

            if (newNetworkVersion)
            {
                if(loadedWeightsValues != null)
                {
                    newNetworks.HiddenLayers = loadedWeightsValues.HiddenLayers;
                    newNetworks.InputLayer = loadedWeightsValues.InputLayer;
                    newNetworks.OutputLayer = loadedWeightsValues.OutputLayer;

                }
                for (int i = 1; i < population; i++)
                {
                    //Spawn childs if wanted and can
                    if (spawnChild && child != null)
                    {
                        SpawnChild();
                    }
                }
            }
            else
            {
                networks[0] = new Network(parameters);

                for (int i = 1; i < population; i++)
                {
                    //Spawn childs if wanted and can
                    if (spawnChild && child != null)
                    {
                        SpawnChild();
                    }
                    if (loadedWeightsValues == null)
                    {
                        networks[i] = new Network(parameters);
                    }
                    else
                    {
                        networks[i] = new Network(parameters);
                        networks[i].setWeights(loadedWeightsValues.weights);
                        if (training)
                        {
                            networks[i] = new Network(this, networks[i]);
                        }
                    }

                }
            }
        }

        public void Sensores(GameObject gameObject = null) {


            if (gameObject == null)
            {
                gameObject = this.gameObject;
            }
            NeuralChildObject _stats = GetStats(gameObject);
            Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();

            RaycastHit hit;
            Vector3 sensorStartPos = gameObject.transform.position+(gameObject.transform.TransformDirection(Vector3.forward) * frontSensorPosition);
            sensorStartPos.y = senoreHight;

            sensors[0] = rigidbody.velocity.magnitude;

            int layerMask = ~(1 << LayerMask.NameToLayer("Car"));
            //front center
            if (Physics.Raycast(sensorStartPos, gameObject.transform.forward, out hit, sensoreLength, layerMask))
            {
                sensors[1] = hit.distance;
                Debug.DrawRay(sensorStartPos, gameObject.transform.forward * hit.distance, Color.yellow);
            }
            else
            {
                sensors[1] = sensoreLength*4;
                Debug.DrawRay(sensorStartPos, gameObject.transform.TransformDirection(Vector3.forward)* sensoreLength, Color.white);
            }
           
            //left
            if (Physics.Raycast(sensorStartPos,Quaternion.AngleAxis(-frontSesnorAngle, gameObject.transform.up) * gameObject.transform.forward, out hit, sensoreLength,layerMask))
            {
                sensors[2] = hit.distance ;
                Debug.DrawRay(sensorStartPos, (Quaternion.AngleAxis(-frontSesnorAngle, gameObject.transform.up) * gameObject.transform.forward) * hit.distance, Color.yellow);
            }
            else
            {
                sensors[2] = sensoreLength;
                Debug.DrawRay(sensorStartPos, (Quaternion.AngleAxis(-frontSesnorAngle, gameObject.transform.up) * gameObject.transform.forward) * sensoreLength);
            }
            
            //left 2
            if (Physics.Raycast(sensorStartPos,Quaternion.AngleAxis(-(frontSesnorAngle/2), gameObject.transform.up) * gameObject.transform.forward, out hit, sensoreLength, layerMask))
            {
                sensors[3] = hit.distance;
                Debug.DrawRay(sensorStartPos, (Quaternion.AngleAxis(-(frontSesnorAngle/2), gameObject.transform.up) * gameObject.transform.forward) * hit.distance, Color.yellow);
            }
            else
            {
                sensors[3] = sensoreLength;
                Debug.DrawRay(sensorStartPos, (Quaternion.AngleAxis(-(frontSesnorAngle / 2), gameObject.transform.up) * gameObject.transform.forward) * sensoreLength);
            }
            //left 3
            if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis(-(frontSesnorAngle * 1.5f), gameObject.transform.up) * gameObject.transform.forward, out hit, sensoreLength, layerMask))
            {
                sensors[8] = hit.distance;
                Debug.DrawRay(sensorStartPos, (Quaternion.AngleAxis(-(frontSesnorAngle * 1.5f), gameObject.transform.up) * gameObject.transform.forward) * hit.distance, Color.yellow);
            }
            else
            {
                sensors[8] = sensoreLength;
                Debug.DrawRay(sensorStartPos, (Quaternion.AngleAxis(-(frontSesnorAngle * 1.5f), gameObject.transform.up) * gameObject.transform.forward) * sensoreLength);
            }

            //right
            if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis(frontSesnorAngle, gameObject.transform.up) * gameObject.transform.forward, out hit, sensoreLength, layerMask))
            {
                
                sensors[4] = hit.distance;
                Debug.DrawRay(sensorStartPos, (Quaternion.AngleAxis(frontSesnorAngle, gameObject.transform.up) * gameObject.transform.forward) * hit.distance, Color.yellow);
            }
            else
            {
                sensors[4] = sensoreLength;
                Debug.DrawRay(sensorStartPos, (Quaternion.AngleAxis(frontSesnorAngle, gameObject.transform.up) * gameObject.transform.forward) * sensoreLength);
            }

            //right 2
            if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis((frontSesnorAngle / 2), gameObject.transform.up) * gameObject.transform.forward, out hit, sensoreLength, layerMask))
            {
                sensors[5] = hit.distance;
                Debug.DrawRay(sensorStartPos, (Quaternion.AngleAxis((frontSesnorAngle / 2), gameObject.transform.up) * gameObject.transform.forward) * hit.distance, Color.yellow);
            }
            else
            {
                sensors[5] = sensoreLength;
                Debug.DrawRay(sensorStartPos, (Quaternion.AngleAxis((frontSesnorAngle / 2), gameObject.transform.up) * gameObject.transform.forward) * sensoreLength);
            }
            //right 2
            if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis((frontSesnorAngle * 1.5f), gameObject.transform.up) * gameObject.transform.forward, out hit, sensoreLength, layerMask))
            {
                sensors[9] = hit.distance;
                Debug.DrawRay(sensorStartPos, (Quaternion.AngleAxis((frontSesnorAngle * 1.5f), gameObject.transform.up) * gameObject.transform.forward) * hit.distance, Color.yellow);
            }
            else
            {
                sensors[9] = sensoreLength;
                Debug.DrawRay(sensorStartPos, (Quaternion.AngleAxis((frontSesnorAngle * 1.5f), gameObject.transform.up) * gameObject.transform.forward) * sensoreLength);
            }

            //backward
            sensorStartPos = sensorStartPos - ((gameObject.transform.TransformDirection(Vector3.forward) * frontSensorPosition) * 2);
            if (Physics.Raycast(sensorStartPos, gameObject.transform.forward*-1, out hit, (sensoreLength /2), layerMask))
            {
                sensors[6] = hit.distance;
                Debug.DrawRay(sensorStartPos, gameObject.transform.forward * -1 * hit.distance, Color.yellow);
            }
            else
            {
                sensors[6] = sensoreLength/2;
                Debug.DrawRay(sensorStartPos, gameObject.transform.TransformDirection(Vector3.forward * -1) * (sensoreLength /2), Color.white);
            }
            sensors[7] = _stats.Speed;
        }

        // Update is called once per frame
        void Update()
        {
                Time.timeScale = timeScale;
                if (!stats.Dead)
                {
                    UpdateCar(this.gameObject);
                }
                else
                {

                    time = Time.time - stats.DriveTime;
                    if (timeLimit != 0)
                    {
                        if (time > timeLimit)
                        {
                            GameOver(gameObject, null, true);
                        }
                    }

                    transform.Rotate(0, 0, 0);
                    transform.Translate(0, 0, 0);
                }
            
        }
        /// <summary>
        /// Spawns childs too process all population at the same time
        /// </summary>
        void SpawnChild()
        {
            childs++;
            if (childs < population)
            {
                GameObject newChild = Instantiate(child) as GameObject;
                
                //set position and rotation right
                newChild.transform.position = startPosition;
                newChild.transform.rotation = startRotation;
                newChild.transform.parent = childFolder.transform;
                NeuralChild childScript = newChild.GetComponent<NeuralChild>();
                childScript.id = childs;
                childScript.parent = this.gameObject;
                gameObjectsChilds[childs] = newChild;
            }
        }

        /// <summary>
        /// Get stats from the gameObject
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        NeuralChildObject GetStats(GameObject gameObject)
        {
            NeuralChild childScript = gameObject.GetComponent<NeuralChild>();
            NeuralChildObject _stats = new NeuralChildObject();
            if (childScript != null)
            {
                _stats = childScript.stats;
            }
            else
            {
                _stats = stats;
            }
            return _stats;
        }

        /// <summary>
        /// Sets stats for the gameObject
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="neuralChildObject"></param>
        void SetStats(GameObject gameObject,NeuralChildObject neuralChildObject)
        {
            NeuralChild childScript = gameObject.GetComponent<NeuralChild>();
            if (childScript != null)
            {
              childScript.stats = neuralChildObject;
            }
            else
            {
                stats= neuralChildObject;
            }

        }

        /// <summary>
        /// Points and Movement.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="id"></param>
        public void UpdateCar(GameObject gameObject)
        {
            Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
            
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = new Vector3(0, 0, 0);

            Sensores(gameObject);
            mutationUpdate();

            //Update stats
            //Used to create points
            NeuralChildObject _stats = GetStats(gameObject);
            time = Time.time - _stats.DriveTime;
            if (timeLimit != 0)
            {
                if (time > timeLimit)
                {
                    GameOver(gameObject,null,true);
                }
            }

            if (_stats.AverageSpeed == 0)
            {
            _stats.AverageSpeed = rigidbody.velocity.magnitude;
            }
            else
            {
            _stats.AverageSpeed = (_stats.AverageSpeed + rigidbody.velocity.magnitude) / 2;
            }

            //End Update stats
            if (newNetworkVersion)
            {
                try
                {
                    results = newNetworks.Compute(sensors);
                }
                catch (Exception)
                {
                    newNetworks = new NeuralNetwork.NetworkModels.Network(parameters[0], new int[] { 8, 8, 8 }, 4, 2, 1);
                }
            }
            else
            {
                try
                {
                    results = networks[_stats.Id].process(sensors);
                }
                catch (Exception)
                {
                    networks[_stats.Id] = new Network(parameters);
                    results = networks[_stats.Id].process(sensors);
                }
            }

            frontForce = (float)results[0];
            backForce = (float)results[1];
            leftForce = (float)results[2];
            leftForce += (float)results[3];
            rightForce = (float)results[4];
            rightForce += (float)results[5];

            if (frontForce >= maxSpeed)
            {
                frontForce = maxSpeed;
            }
            if (backForce >= maxSpeed)
            {
                backForce = maxSpeed;
            }

            _stats.Speed += (frontForce/14);
            _stats.Speed -= (backForce/8)/ (_stats.Speed*16);
            speed = _stats.Speed;
            if (_stats.Speed <= -maxSpeed)
            {
                _stats.Speed = -maxSpeed;
            }
            else if (_stats.Speed >= maxSpeed)
            {
                _stats.Speed = maxSpeed;
            }

            _stats.DriveDistance = Vector3.Distance(gameObject.transform.position, startPosition);
            
            _stats.LastPosition = gameObject.transform.position;

            gameObject.transform.Rotate(0, (leftForce- rightForce)*4, 0);

            gameObject.transform.Translate(0, 0, _stats.Speed);

            //gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, new Vector3(0,0, _stats.speed), 0.5f * Time.deltaTime);
            _stats.SenoreHistory.Add(sensors);
            _stats.ResultHistory.Add(results);
            SetStats(gameObject, _stats);
            if (ignoreCollide)
                ignoreCollide = false;
        }

        //game over, friend :/
        void OnCollisionEnter(Collision col)
        {
            if (!stats.Dead)
            {
                GameOver(this.gameObject, col);
            }
        }

        public NeuralChildObject AddPoints(Collision col, NeuralChildObject _stats,out bool pointAdded)
        {
            if (col != null)
            {
                if (col.collider.gameObject.tag == "Point")
                {
                    if (!_stats.Points.Contains(col.collider.gameObject))
                    {
                        _stats.Points.Add(col.collider.gameObject);
                    }
                    pointAdded = true;
                    return _stats;
                }
                if (col.collider.gameObject.tag == "Street")
                {
                    if (!_stats.Streets.Contains(col.collider.gameObject))
                    {
                        _stats.Streets.Add(col.collider.gameObject);
                    }
                    pointAdded = true;
                    return _stats;
                }
            }
            pointAdded = false;
            return _stats;
        }


        public void GameOver(GameObject gameObject, Collision col = null,bool timeOver=false) {
            try
            {
                NeuralChildObject _stats = GetStats(gameObject);

                if (col == null && !timeOver)
                {
                    return;
                }
                else if(!timeOver)
                {
                    bool pointAdded = false;
                    _stats = AddPoints(col, _stats,out pointAdded);
                    if (pointAdded)
                    {
                        return;
                    }
                    if (col.gameObject.tag == "Car") return;
                }

                if (ignoreCollide) return;

                bool reproduce = false;

                if ((_stats.Dead == false) || timeOver)
                {
                    ReadPoints(_stats);

                    if (spawnChild && child != null)
                    {
                        deadChilds++;
                        if (!timeOver && newNetworkVersion)
                        {
                            while (newNetworks.Compute(sensors) == results)
                            {
                                newNetworks.BackPropagate(results);
                                newNetworks.BackPropagate(results);
                            }
                        }
                        _stats.Dead = true;
                        SetStats(gameObject, _stats);
                        if (deadChilds >= childs+1)
                        {
                            if (timeOver)
                            {
                                print("[" + transform.root.gameObject.name + "][ResetReason] TimeOver");
                                //get stats of living childs
                                foreach (GameObject childgo in gameObjectsChilds)
                                {
                                    if (childgo != null)
                                    {
                                        _stats = GetStats(childgo);
                                        ReadPoints(_stats);
                                    }
                                }
                            }
                            else
                            {
                                print("[" + transform.root.gameObject.name + "][ResetReason] All childs dead");
                            }
                            reproduce = true;
                        }
                    }
                    else
                    {
                        if (id > population)
                        {
                            reproduce = true;
                        }
                        else
                        {
                            id++;
                            print("[" + transform.root.gameObject.name + "][ResetReason] Car dead");
                            ResetCarPosition(gameObject);
                        }
                    }
                }
                else
                {
                    _stats.Dead = true;
                    SetStats(gameObject, _stats);
                    return;
                }
                //now we reproduce
                if (reproduce)
                {
                    double maxValue = points[0];
                    int maxIndex = 0;
                    List<Network> cache = new List<Network>();
                    //looking for the two best networks in the generation
                    for (int i = 1; i < population; i++)
                    {
                        if (points[i] > maxValue)
                        {
                            maxIndex = i;
                            maxValue = points[i];
                        }
                    }
                    cache.Add(networks[maxIndex]);
                    cache.Add(networks[maxIndex]);

                    cache.Add(new Network(parameters));
                    if (newNetworkVersion)
                    {
                        for (int i = 0; i < _stats.ResultHistory.Count; i++)
                        {
                            newNetworks.ForwardPropagate(_stats.SenoreHistory[i]);
                        }

                        for (int i = 0; i < population; i++)
                        {
                            points[i] = 0;
                        }
                    }
                    else
                    {
                        if (points[maxIndex] > bestDistance)
                        {
                            bestDistance = (float)points[maxIndex];
                        }
                        lastBest = networks[maxIndex];

                        if (bestNetwork == null)
                        {
                            bestNetwork = networks[0];
                        }
                        cache.Add(lastBest);
                        cache.Add(bestNetwork);
                        networkUpdateStatus = 0;
                        if (training)
                        {
                            UpdateNetwork(cache);
                        }
                        else
                        {
                            ReviveChilds();
                        }
                        
                    }
                }
            }
            catch (Exception e)
            {
                print(e.StackTrace);
            }
            return;
        }
        void NextGen()
        {
            generation++;
            id = 0;

            if (saveBestNetwork)
            {
                SaveBestNetwork();
            }

            deadChilds = 0;
            if (spawnChild && child != null)
            {
                ReviveChilds();
            }
            Debug.Log("[" + transform.root.gameObject.name + "] generation " + generation + " is born");
        }

        void UpdateNetwork(List<Network> _networks) {
            for (int i = 0; i < population; i++)
            {
                if (networkUpdateStatus >= population-1)
                {
                    NextGen();
                    networkUpdateStatus = 0;
                    print("Updated " + i + " Networks");
                    return;
                }
                else
                {
                    print("Update " + i + " Network");
                    points[networkUpdateStatus] = 0;
                    //creating new generation of networks with random combinations of genes from two best parents
                    networks[networkUpdateStatus] = new Network(this, _networks.ToArray());
                    networkUpdateStatus++;
                }
            }
        }

        void ReadPoints(NeuralChildObject _stats)
        {
            float score = 0;
            switch (fitnessMeasure)
            {
                case FitnessMeasure.distance:
                    score = _stats.DriveDistance;
                    break;
                case FitnessMeasure.distanceByTime:
                    score = _stats.DriveTime;
                    break;
                case FitnessMeasure.distance2byTime:
                    score = _stats.DriveTime + _stats.DriveDistance;
                    break;
                case FitnessMeasure.speed:
                    score = _stats.AverageSpeed;
                    break;
                case FitnessMeasure.streetParts:
                    score = _stats.Streets.Count;
                    break;
                case FitnessMeasure.points:
                    score = _stats.Points.Count;
                    break;
            }
            points[_stats.Id] = score;
            if(bestScore < score)
            {
                saveBestNetwork = true;
                bestScore = score;
                bestNetwork = networks[_stats.Id];
                bestStats = _stats;
                SetColor(gameObjectsChilds[_stats.Id], Color.green);

                    if (newNetworkVersion)
                    {
                        List<NeuralNetwork.NetworkModels.DataSet> dataSets = new List<NeuralNetwork.NetworkModels.DataSet>();
                        for (int i = 0; i < _stats.ResultHistory.Count; i++)
                        {
                            dataSets.Add(new NeuralNetwork.NetworkModels.DataSet(_stats.SenoreHistory[i], _stats.ResultHistory[i]));
                        }
                        newNetworks.Train(dataSets, 0.5);
                    }
            }
        }

        void SaveBestNetwork()
        {
            try
            {
                string m_Path = Application.dataPath + "/" + transform.root.gameObject.name + ".xml";
                NeuralWeights neuralWeights = new NeuralWeights();
                neuralWeights.bestScore = (float)bestScore;
                neuralWeights.Generation = generation;
                //Save this Network
                if (!newNetworkVersion)
                {
                    neuralWeights.weights = networks[0].getWeigths();
                    
                }
                else
                {
                    neuralWeights.HiddenLayers = newNetworks.HiddenLayers;
                    neuralWeights.InputLayer = newNetworks.InputLayer;
                    neuralWeights.OutputLayer = newNetworks.OutputLayer;
                }
                SaveWeights(m_Path, neuralWeights);
            }
            catch (Exception)
            {
                print("[" + transform.root.gameObject.name + "] Save error");
            }
        }

        void ReviveChilds()
        {
            foreach(GameObject childgo in gameObjectsChilds)
            {
                if(childgo != null)
                {
                    NeuralChild childScript = childgo.GetComponent<NeuralChild>();
                    if (childScript != null)
                    {
                        childScript.Revive();
                    }
                    else
                    {
                        ResetCarPosition(this.gameObject);
                        stats.Dead = false;
                    }
                }
            }
        }

        void SetColor(GameObject gameObject, Color color)
        {

            foreach (Renderer r in gameObject.GetComponentsInChildren<Renderer>())
            {
                r.material.color = color;
            }

        }

        //TODO: sometimes the velocity is not reseted.. for some reason
        public void ResetCarPosition(GameObject gameObject)
        {
            
            Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();

            NeuralChildObject _stats = GetStats(gameObject);
            _stats = ResetStats(_stats);

            if (childRespawnAtParent)
            {
                gameObject.transform.position = startPosition;
                _stats.LastPosition = startPosition;
            }
            else
            {
                gameObject.transform.position = _stats.LastPosition;
            }

            SetStats(gameObject, _stats);

            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = new Vector3(0, 0, 0);

            gameObject.transform.rotation = startRotation;
            SetColor(gameObject, Color.white);
        }

        NeuralChildObject ResetStats(NeuralChildObject _stats)
        {
            _stats.AverageSpeed = 0;
            _stats.DriveDistance = 0;
            _stats.DriveTime = Time.time;
            _stats.Speed = 0f;
            _stats.Streets.Clear();
            _stats.ResultHistory.Clear();
            _stats.SenoreHistory.Clear();
            return _stats;
        }

        void SaveWeights(string path, NeuralWeights neuralWeights)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                XmlSerializer xSer = new XmlSerializer(typeof(NeuralWeights));

                xSer.Serialize(fs, neuralWeights);
            }
        }
        public NeuralWeights Load(string path)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open)) //double check that...
                {
                    XmlSerializer _xSer = new XmlSerializer(typeof(NeuralWeights));

                    return (NeuralWeights)(_xSer.Deserialize(fs));
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
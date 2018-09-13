using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Xml.Serialization;
namespace AssemblyCSharp
{
    public enum FitnessMeasure
    {
        distance,
        distance2byTime,
        distanceByTime,
        speed
    }

    public class AIController : MonoBehaviour
    {


        Rigidbody rigidbody;

        public FitnessMeasure fitnessMeasure;

        float lastSpeed = 0;
        bool isColliding = true;

        public int timeScale;

        public int population = 10;
        public static int staticPopulation;

        public double driveTime = 0;
        public double driveDistance = 0;
        public float frontForce, backForce, leftForce, rightForce;

        public static int mutationRateStatic;
        public int mutationRate;
        public static int generation = 0;
        public double[] points;
        public double[] results;
        public double[] sensors;
        [Header("Sensor Settings")]
        public float sensoreLength = 3f;
        public float frontSensorPosition = 0.5f;
        public float frontSideSensorPosition = 0.2f;
        public float frontSesnorAngle = 0.5f;

        public static int currentNeuralNetwork = 0;

        public static float bestDistance = 0;

        public bool ignoreFirstCollide = true;
        
        //layers (input, hidden, output)
        int[] parameters = { 7, 16, 4 };

        Network[] networks;
        RaycastHit hit;

        Vector3 position;
        Vector3 lastPosition;
        Vector3 startPosition;

        void mutationUpdate()
        {
            mutationRateStatic = mutationRate;
            //Debug.Log ("updated!");
        }

        // Use this for initialization
        void Start()
        {
            InvokeRepeating("mutationUpdate", 1, 1);
            startPosition = transform.position;
            lastPosition = startPosition;
            
            staticPopulation = population;

            Time.timeScale = timeScale;

            Debug.Log("[" + transform.root.gameObject.name + "] Generation " + generation);

            rigidbody = GetComponent<Rigidbody>();

            results = new double[5];
            points = new double[population];
            sensors = new double[7];




            position = transform.position;
            networks = new Network[population];


            for (int i = 0; i < population; i++)
            {
                networks[i] = new Network(parameters);
                string m_Path = Application.dataPath + "/" + transform.root.gameObject.name + ".xml";
                print(m_Path);
                var loadedWeights = Load(m_Path);
                if (loadedWeights != null)
                {
                    networks[i].setWeights(loadedWeights);
                    print("["+transform.root.gameObject.name + "] LoadedWeights");
                }
            }
        }


        void FixedUpdate()
        {
            Sensores();
        }

        void Sensores() {
            RaycastHit hit;
            Vector3 sensorStartPos = transform.position;
            sensorStartPos.z += frontSensorPosition;
            //front center
            if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensoreLength)) {

            }

            //front right angle sensor
            sensorStartPos.x += frontSensorPosition;
            if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis(frontSesnorAngle, transform.up)*transform.forward, out hit, sensoreLength))
            {

            }
            Debug.DrawLine(sensorStartPos, hit.point);

            //front right
            sensorStartPos.x += frontSensorPosition;
            if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensoreLength))
            {

            }
            Debug.DrawLine(sensorStartPos, hit.point);


            //front left angle sensor
            sensorStartPos.x += frontSensorPosition;
            if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis(-frontSesnorAngle, transform.up) * transform.forward, out hit, sensoreLength))
            {

            }
            Debug.DrawLine(sensorStartPos, hit.point);

            //front left
            sensorStartPos.x += frontSensorPosition;
            if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensoreLength))
            {

            }
            Debug.DrawLine(sensorStartPos, hit.point);
        }

        // Update is called once per frame
        void Update()
        {

            driveDistance += (Math.Abs(lastPosition.x - transform.position.x) + Math.Abs(lastPosition.y - transform.position.y) + Math.Abs(lastPosition.z - transform.position.z)) / 4;
            lastPosition = transform.position;
            driveTime += Time.deltaTime;

            isColliding = false;


            if (transform.position.y > 5)
                OnCollisionEnter(null);



            //20 should be maximum force
            sensors[0] = transform.position.y / 5.0f;


                sensors[1] = transform.eulerAngles.x / 45.0f;
                sensors[2] = -(transform.eulerAngles.x - 360) / 45.0f;
                sensors[3] = transform.position.x * 2500000;
                sensors[4] = -(transform.position.x * 2500000);
                sensors[5] = transform.eulerAngles.x / 90.0f;
                sensors[6] = -(transform.eulerAngles.x - 360) / 90.0f;


            try
            {
                results = networks[currentNeuralNetwork].process(sensors);
            }
            catch (Exception e) {
                if (currentNeuralNetwork != 0)
                {
                    try
                    {
                        networks[currentNeuralNetwork] = networks[currentNeuralNetwork - 1];
                    }
                    catch (Exception ex)
                    {
                        networks[currentNeuralNetwork] = new Network(parameters);
                    }

                }
                else
                {
                    networks[currentNeuralNetwork] = new Network(parameters);
                }
                results = networks[currentNeuralNetwork].process(sensors);
            }

            frontForce = (float)results[0];
            backForce = (float)results[1];
            leftForce = (float)results[2];
            rightForce = (float)results[3];
            //front.AddRelativeForce(new Vector3(0, frontForce));
            //right.AddRelativeForce(new Vector3(0, rightForce));
            //left.AddRelativeForce(new Vector3(0, leftForce));
            //back.AddRelativeForce(new Vector3(0, backForce));
            float speed = (frontForce / 4) - (backForce / 4);
            if (lastSpeed == speed)
            {

            }
            else
            {
                lastSpeed = speed;
            }

            transform.Rotate(0, ((leftForce / 8) - (rightForce / 8)), 0);
            transform.Translate(0, 0, speed);

        }


        //game over, friend :/
        void OnCollisionEnter(Collision col)
        {
            try
            {
                if (col.collider.gameObject.name == "Reifen" || col.collider.gameObject.tag == "Street") return;
            }
            catch (Exception e) { }
            if (isColliding) return;
            isColliding = true;

            resetCarPosition();

            switch (fitnessMeasure)
            {
                case FitnessMeasure.distance:
                    points[currentNeuralNetwork] = driveDistance;
                    break;
                case FitnessMeasure.distanceByTime:
                    points[currentNeuralNetwork] = driveTime;
                    break;
                case FitnessMeasure.distance2byTime:
                    points[currentNeuralNetwork] = driveTime + driveDistance;
                    break;
                case FitnessMeasure.speed:
                    points[currentNeuralNetwork] = driveDistance/ driveTime;
                    break;
            }


            driveDistance = 0;
            driveTime = 0;

            //Debug.Log("network " + currentNeuralNetwork + " scored " + points[currentNeuralNetwork]);

            string m_Path = Application.dataPath + "/" + transform.root.gameObject.name + ".xml";
            SaveWeights(m_Path);

            //now we reproduce
            if (currentNeuralNetwork == population - 1)
            {
                double maxValue = points[0];
                int maxIndex = 0;

                //looking for the two best networks in the generation

                for (int i = 1; i < population; i++)
                {
                    if (points[i] > maxValue)
                    {
                        maxIndex = i;
                        maxValue = points[i];
                    }
                }


                if (points[maxIndex] > bestDistance)
                {

                    bestDistance = (float)points[maxIndex];

                }

                points[maxIndex] = -10;

                Network mother = networks[maxIndex];

          
                maxValue = points[0];
                maxIndex = 0;

                for (int i = 1; i < population; i++)
                {
                    if (points[i] > maxValue)
                    {
                        maxIndex = i;
                        maxValue = points[i];
                    }
                }

                points[maxIndex] = -10;

                Network father = networks[maxIndex];


                for (int i = 0; i < population; i++)
                {
                    points[i] = 0;
                    //creating new generation of networks with random combinations of genes from two best parents
                    networks[i] = new Network(father, mother);
                }

                generation++;
                Debug.Log("[" + transform.root.gameObject.name + "] generation " + generation + " is born");

                //because we increment it at the beginning, that's why -1
                currentNeuralNetwork = -1;
            }

            currentNeuralNetwork++;
            //position reset is pretty important, don't forget it :*
            position = transform.position;
        }

        //TODO: sometimes the velocity is not reseted.. for some reason
        void resetCarPosition()
        {
            rigidbody.velocity = Vector3.zero;
            transform.position = startPosition;
            transform.rotation = new Quaternion(0, 0, 0, 0);
            rigidbody.angularVelocity = new Vector3(0, 0, 0);

        }
        void SaveWeights(string path)
        {
            NeuralWeights neuralWeights = new NeuralWeights();
            neuralWeights.weights = networks[currentNeuralNetwork].getWeigths();
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                XmlSerializer xSer = new XmlSerializer(typeof(NeuralWeights));

                xSer.Serialize(fs, neuralWeights);
            }
        }
        public double[][][] Load(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open)) //double check that...
            {
                XmlSerializer _xSer = new XmlSerializer(typeof(NeuralWeights));

                return ((NeuralWeights)(_xSer.Deserialize(fs))).weights;
            }
        }



    }
}
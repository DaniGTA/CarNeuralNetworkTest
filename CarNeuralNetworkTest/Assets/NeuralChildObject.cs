using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AssemblyCSharp
{
    [System.Serializable]
    public class NeuralChildObject
    {
        [SerializeField] public int Id { get; set; }
        [SerializeField] public float DriveDistance { get; set; }
        [SerializeField] public Vector3 LastPosition { get; set; }
        [SerializeField] public float DriveTime { get; set; }
        [SerializeField] public float AverageSpeed {get;set;}
        [SerializeField] public float Speed { get; set; }
        [SerializeField] public bool Dead { get; set; }
        [SerializeField] public List<GameObject> Streets { get; set; }
        [SerializeField] public List<GameObject> Points { get; set; }

        /// <summary>
        /// Can be used to Train the AI
        /// </summary>
        public List<double[]> SenoreHistory { get; set; }
        public List<double[]> ResultHistory { get; set; }

        public NeuralChildObject()
        {
            Id = 0;
            DriveDistance = 0f;
            LastPosition = new Vector3();
            DriveTime = 0f;
            AverageSpeed = 0f;
            Speed = 0f;
            Dead = false;
            Streets = new List<GameObject>();
            Points = new List<GameObject>();
            SenoreHistory = new List<double[]>();
            ResultHistory = new List<double[]>();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AssemblyCSharp
{
    class NeuralChild : MonoBehaviour
    {
        public int id = 1;
        public GameObject parent;
        public NeuralChildObject stats;

        AIController parentScript;
        void Start()
        {
            stats = new NeuralChildObject();
            stats.LastPosition = transform.position;
        }

        void Update()
        {
            if (id != stats.Id)
            {
                stats.Id = id;
            }
            else
            {

                if (parentScript == null)
                {
                    if (parent != null)
                    {
                        parentScript = parent.GetComponent<AIController>();
                    }
                }
                else
                {
                    if (!stats.Dead)
                    {
                        parentScript.UpdateCar(this.gameObject);
                    }
                    else
                    {
                        transform.Rotate(0, 0, 0);
                        transform.Translate(0, 0, 0);
                    }
                }
            }
        }

        void OnCollisionEnter(Collision col)
        {
            if (id != stats.Id)
            {
                stats.Id = id;
            }
            else
            {
                if (parentScript != null)
                {
                    if (!stats.Dead)
                    {
                        parentScript.GameOver(this.gameObject, col);
                    }
                }
            }
        }
        public void Revive()
        {
            parentScript.ResetCarPosition(this.gameObject);
            stats.Dead = false;
        }
    }
}

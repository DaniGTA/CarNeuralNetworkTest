using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace AssemblyCSharp
{
    public class Network
    {

        double[][][] weights;
        public int[] parameters;

        public int lenght;

        public double[][][] getWeigths()
        {
            return weights;
        }
        public void setWeights(double[][][] weigths)
        {
            this.weights = weigths;
        }

        double sigmoid(double x)
        {
            return 1 / (1 + Mathf.Exp(-(float)x));
        }

        void initializeVariables()
        {
            this.weights = new double[parameters.Length - 1][][];
            this.lenght = parameters.Length;
        }

        public Network(AIController aIController,params Network[] networks)
        {

            this.parameters = networks[0].parameters;
            initializeVariables();

            for (int i = 0; i < parameters.Length - 1; i++)
            {

                weights[i] = new double[parameters[i]][];

                for (int j = 0; j < parameters[i]; j++)
                {

                    weights[i][j] = new double[parameters[i + 1]];

                    for (int k = 0; k < parameters[i + 1]; k++)
                    {
                        double[] weight = new double[networks.Length];
                        for (int e = 0; e < networks.Length; e++)
                        {
                            weight[e] = networks[e].weights[i][j][k];
                        }
                        weights[i][j][k] = Random.Range((float)weight.Min(),(float)weight.Max());
                    }
                }
            }

            for (int i = 0; i < aIController.mutationRateStatic; i++)
            {
                //Debug.Log ("mutating!");
                int mutationLayer = Random.Range(0, weights.Length);
                int mutationLeft = Random.Range(0, weights[mutationLayer].Length);
                int mutationRight = Random.Range(0, weights[mutationLayer][mutationLeft].Length);

                weights[mutationLayer][mutationLeft][mutationRight] = getRandomWeight();
            }
            //Debug.Log (mutationLayer + " " + mutationLeft + " " + mutationRight);
        }


        public Network(int[] parameters)
        {
            this.parameters = parameters;
            //int a = 0;
            //{3,5,2}

            initializeVariables();

            for (int i = 0; i < parameters.Length - 1; i++)
            {

                weights[i] = new double[parameters[i]][];

                for (int j = 0; j < parameters[i]; j++)
                {

                    weights[i][j] = new double[parameters[i + 1]];

                    for (int k = 0; k < parameters[i + 1]; k++)
                    {

                        weights[i][j][k] = getRandomWeight();
                        //a++;
                        //Debug.Log (a);
                    }
                }
            }
        }

        public double[] process(double[] inputs)
        {
            //int a = 0;

            if (inputs.Length != parameters[0])
            {

                Debug.Log("wrong input lenght!");
                return null;
            }

            double[] outputs;
            //Debug.Log (lenght);
            //for each layer
            for (int i = 0; i < (lenght - 1); i++)
            {

                //output values, they all start at 0 by default, checked that in C# Documentation ;)
                outputs = new double[parameters[i + 1]];


                //for each input neuron
                for (int j = 0; j < inputs.Length; j++)
                {

                    //and for each output neuron
                    for (int k = 0; k < outputs.Length; k++)
                    {
                        //Debug.Log (i + " " + j + " " + k);
                        //a++;
                        //increase the load of an output neuron by the value of each input neuron multiplied by the weight between them
                        outputs[k] += inputs[j] * weights[i][j][k];
                    }
                }

                //we have the proper output values, now we have to use them as inputs to the next layer and so on, until we hit the last layer
                inputs = new double[outputs.Length];

                //after all output neurons have their values summed up, apply the activation function and save the value into new inputs
                for (int l = 0; l < outputs.Length; l++)
                {
                    //this MIGHT be considered a little of a cheat, but I'm a beginner. So i hope you don't mind that multiplication by 5
                    inputs[l] = sigmoid(outputs[l] * 5);
                    //Debug.Log ("i " + inputs [l]);
                }



            }

            //Debug.Log (a);

            return inputs;


            // inputy to wartości odległości z czujników (0-1), są 3 (na razie)

            //outputy to wartości do sterowania (zakręt i silnik), są 2

            //old way of processing, not working
            //return processRecurrent (inputs, 0);
        }

        double getRandomWeight()
        {
            return Random.Range(-1.0f, 1.0f);
        }

    }
}

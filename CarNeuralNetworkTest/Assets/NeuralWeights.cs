using NeuralNetwork.NetworkModels;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace AssemblyCSharp
{
	[Serializable]
	public class NeuralWeights
	{
        //ForOldNetwork
		public double[][][] weights {get;set;}
        public int Generation { get; set; }

        //ForNewnetwork
        public List<Neuron> InputLayer { get; set; }
        public List<List<Neuron>> HiddenLayers { get; set; }
        public List<Neuron> OutputLayer { get; set; }
        public float bestScore { get; set; }
    }
}


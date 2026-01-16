using System;
using UnityEngine;

// [CreateAssetMenu(fileName = "Palette", menuName = "Theming/Palette", order = 1)]
public class Palette : ScriptableObject
{
    public Colors colors;
    public Gradients gradients;

    [Serializable]
    public struct Colors
    {
        public Color primary;
        public Color secondary;
        public Color labels;
        public Color background;
    }
    
    [Serializable]
    public struct Gradients
    {
        public Gradient primary;
        public Gradient secondary;
        public Gradient labels;
        public Gradient background;
        public Gradient effects;
    }
}

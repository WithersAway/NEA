using System;
using System.Collections.Generic;
using Avalonia.Controls.Shapes;

namespace NEA
{
    public class Obstacle
    {
        internal Rectangle obstacle { get; set; }
        public Obstacle(Rectangle rectangleParameter)
        {
            obstacle = rectangleParameter;
        }

    }
}
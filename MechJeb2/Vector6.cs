using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class Vector6 : IConfigNode
    {
        public Vector3d positive = Vector3d.zero, negative = Vector3d.zero;

        public enum Direction { FORWARD, BACK, UP, DOWN, RIGHT, LEFT };
        public static Dictionary<Direction, Vector3d> directions = new Dictionary<Direction, Vector3d> {
            { Direction.FORWARD, Vector3d.forward },
            { Direction.BACK, Vector3d.back },
            { Direction.UP, Vector3d.up },
            { Direction.DOWN, Vector3d.down },
            { Direction.RIGHT, Vector3d.right },
            { Direction.LEFT, Vector3d.left }
        };

        public double forward { get { return positive.z; } set { positive.z = value; } }
        public double back { get { return negative.z; } set { negative.z = value; } }
        public double up { get { return positive.y; } set { positive.y = value; } }
        public double down { get { return negative.y; } set { negative.y = value; } }
        public double right { get { return positive.x; } set { positive.x = value; } }
        public double left { get { return negative.x; } set { negative.x = value; } }

        public double this[Direction index]
        {
            get
            {
                switch (index)
                {
                    case Direction.FORWARD:
                        return forward;
                    case Direction.BACK:
                        return back;
                    case Direction.UP:
                        return up;
                    case Direction.DOWN:
                        return down;
                    case Direction.RIGHT:
                        return right;
                    case Direction.LEFT:
                        return left;
                }
                return 0;
            }
            set
            {
                switch (index)
                {
                    case Direction.FORWARD:
                        forward = value;
                        break;
                    case Direction.BACK:
                        back = value;
                        break;
                    case Direction.UP:
                        up = value;
                        break;
                    case Direction.DOWN:
                        down = value;
                        break;
                    case Direction.RIGHT:
                        right = value;
                        break;
                    case Direction.LEFT:
                        left = value;
                        break;
                }
            }
        }

        public Vector6() { }
        public Vector6(Vector3d positive, Vector3d negative)
        {
            this.positive = positive;
            this.negative = negative;
        }

        public void Add(Vector3d vector)
        {
            foreach (Direction d in Enum.GetValues(typeof(Direction)))
            {
                double projection = Vector3d.Dot(vector, directions[d]);
                if (projection > 0)
                {
                    this[d] += projection;
                }
            }
        }

        public double GetMagnitude(Vector3d direction)
        {
            double sqrMagnitude = 0;
            foreach (Direction d in Enum.GetValues(typeof(Direction)))
            {
                double projection = Vector3d.Dot(direction.normalized, directions[d]);
                if (projection > 0)
                {
                    sqrMagnitude += Math.Pow(projection * this[d], 2);
                }
            }
            return Math.Sqrt(sqrMagnitude);
        }

        public void Load(ConfigNode node)
        {
            if (node.HasValue("positive"))
            {
                positive = KSPUtil.ParseVector3d(node.GetValue("positive"));
            }
            if (node.HasValue("negative"))
            {
                negative = KSPUtil.ParseVector3d(node.GetValue("negative"));
            }
        }

        public void Save(ConfigNode node)
        {
            node.SetValue("positive", KSPUtil.WriteVector(positive));
            node.SetValue("negative", KSPUtil.WriteVector(negative));
        }
    }
}

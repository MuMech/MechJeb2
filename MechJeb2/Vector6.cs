extern alias JetBrainsAnnotations;
using System;
using JetBrainsAnnotations::JetBrains.Annotations;

namespace MuMech
{
    public class Vector6 : IConfigNode
    {
        public Vector3d Positive = Vector3d.zero, Negative = Vector3d.zero;

        public enum Direction { FORWARD = 0, BACK = 1, UP = 2, DOWN = 3, RIGHT = 4, LEFT = 5 }

        public static readonly Vector3d[] Directions = { Vector3d.forward, Vector3d.back, Vector3d.up, Vector3d.down, Vector3d.right, Vector3d.left };

        public static readonly Direction[] Values = (Direction[])Enum.GetValues(typeof(Direction));

        [UsedImplicitly]
        public double Forward
        {
            get => Positive.z;
            set => Positive.z = value;
        }

        [UsedImplicitly]
        public double Back
        {
            get => Negative.z;
            set => Negative.z = value;
        }

        [UsedImplicitly]
        public double Up
        {
            get => Positive.y;
            set => Positive.y = value;
        }

        [UsedImplicitly]
        public double Down
        {
            get => Negative.y;
            set => Negative.y = value;
        }

        [UsedImplicitly]
        public double Right
        {
            get => Positive.x;
            set => Positive.x = value;
        }

        [UsedImplicitly]
        public double Left
        {
            get => Negative.x;
            set => Negative.x = value;
        }

        [UsedImplicitly]
        public double this[Direction index]
        {
            get =>
                index switch
                {
                    Direction.FORWARD => Forward,
                    Direction.BACK    => Back,
                    Direction.UP      => Up,
                    Direction.DOWN    => Down,
                    Direction.RIGHT   => Right,
                    Direction.LEFT    => Left,
                    _                 => 0
                };
            set
            {
                switch (index)
                {
                    case Direction.FORWARD:
                        Forward = value;
                        break;
                    case Direction.BACK:
                        Back = value;
                        break;
                    case Direction.UP:
                        Up = value;
                        break;
                    case Direction.DOWN:
                        Down = value;
                        break;
                    case Direction.RIGHT:
                        Right = value;
                        break;
                    case Direction.LEFT:
                        Left = value;
                        break;
                }
            }
        }

        public Vector6() { }

        public Vector6(Vector3d positive, Vector3d negative)
        {
            Positive = positive;
            Negative = negative;
        }

        public void Reset()
        {
            Positive = Vector3d.zero;
            Negative = Vector3d.zero;
        }

        public void Add(Vector3d vector)
        {
            for (int i = 0; i < Values.Length; i++)
            {
                Direction d = Values[i];
                double projection = Vector3d.Dot(vector, Directions[(int)d]);
                if (projection > 0)
                {
                    this[d] += projection;
                }
            }
        }

        public double GetMagnitude(Vector3d direction)
        {
            double sqrMagnitude = 0;
            for (int i = 0; i < Values.Length; i++)
            {
                Direction d = Values[i];
                double projection = Vector3d.Dot(direction.normalized, Directions[(int)d]);
                if (projection > 0)
                {
                    sqrMagnitude += Math.Pow(projection * this[d], 2);
                }
            }

            return Math.Sqrt(sqrMagnitude);
        }

        public double MaxMagnitude() => Math.Max(Positive.MaxMagnitude(), Negative.MaxMagnitude());

        public void Load(ConfigNode node)
        {
            if (node.HasValue("positive"))
            {
                Positive = KSPUtil.ParseVector3d(node.GetValue("positive"));
            }

            if (node.HasValue("negative"))
            {
                Negative = KSPUtil.ParseVector3d(node.GetValue("negative"));
            }
        }

        public void Save(ConfigNode node)
        {
            node.SetValue("positive", KSPUtil.WriteVector(Positive));
            node.SetValue("negative", KSPUtil.WriteVector(Negative));
        }
    }
}

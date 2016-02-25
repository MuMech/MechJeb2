using System;
using Smooth.Algebraics;

namespace Smooth.Slinq.Context {
	public delegate void Mutator<T, C>(ref C context, out Option<T> next);
}
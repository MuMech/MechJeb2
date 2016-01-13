using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Smooth.Collections;

namespace Smooth.Compare.Comparers {
	/// <summary>
	/// Fast, allocation free equality comparer for blittable structs with an underlying size of 32 bits or less.
	/// </summary>
	public class Blittable32EqualityComparer<T> : Smooth.Collections.EqualityComparer<T> {
		public override bool Equals(T t1, T t2) {
			Converter converter;
			converter.value = 0;
			converter.t = t1;
			var v1 = converter.value;
			converter.t = t2;
			return v1 == converter.value;
		}
		
		public override int GetHashCode(T t) {
			Converter converter;
			converter.value = 0;
			converter.t = t;
			return converter.value;
		}
		
		[StructLayout(LayoutKind.Explicit)]
		internal struct Converter
		{
			[FieldOffset(0)]
			public T t;
			
			[FieldOffset(0)]
			public Int32 value;
		}
	}

	/// <summary>
	/// Fast, allocation free equality comparer for blittable structs with an underlying size of 64 bits or less.
	/// </summary>
	public class Blittable64EqualityComparer<T> : Smooth.Collections.EqualityComparer<T> {
		public override bool Equals(T t1, T t2) {
			Converter converter;
			converter.value = 0;
			converter.t = t1;
			var v1 = converter.value;
			converter.t = t2;
			return v1 == converter.value;
		}
		
		public override int GetHashCode(T t) {
			Converter converter;
			converter.value = 0;
			converter.t = t;
			return converter.value.GetHashCode();
		}
		
		[StructLayout(LayoutKind.Explicit)]
		internal struct Converter
		{
			[FieldOffset(0)]
			public T t;
			
			[FieldOffset(0)]
			public Int64 value;
		}
	}

//	/// <summary>
//	/// Fast, allocation free IEqualityComparer<T> for Enums that uses System.Reflection.Emit to create JIT complied equality and hashCode functions.
//	/// 
//	/// Note: This class isn't any faster than Blittable32EqualityComparer or Blittable64EqualityComparer and doesn't work on platforms without JIT complilation.
//	/// 
//	/// It is provided simply as example code.
//	/// </summary>
//	public class EnumEmitEqualityComparer<T> : Smooth.Collections.EqualityComparer<T> {
//		private readonly Func<T, T, bool> equals;
//		private readonly Func<T, int> hashCode;
//		
//		public EnumEmitEqualityComparer() {
//			var type = typeof(T);
//			
//			if (type.IsEnum) {
//				var l = Expression.Parameter(type, "l");
//				var r = Expression.Parameter(type, "r");
//				
//				this.equals = Expression.Lambda<Func<T, T, bool>>(Expression.Equal(l, r), l, r).Compile();
//				
//				switch (Type.GetTypeCode(type)) {
//				case TypeCode.Int64:
//				case TypeCode.UInt64:
//					this.hashCode = Expression.Lambda<Func<T, int>>(Expression.Call(Expression.Convert(l, typeof(Int64)), typeof(Int64).GetMethod("GetHashCode")), l).Compile();
//					break;
//				default:
//					this.hashCode = Expression.Lambda<Func<T, int>>(Expression.Convert(l, typeof(Int32)), l).Compile();
//					break;
//				}
//			} else {
//				throw new ArgumentException(GetType().Name + " can only be used with enum types.");
//			}
//		}
//		
//		public override bool Equals(T t1, T t2) {
//			return equals(t1, t2);
//		}
//		
//		public override int GetHashCode(T t) {
//			return hashCode(t);
//		}
//	}
}

using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace KodeUI {
	public static class KodeUI_Utils
	{
		public static TEnum ToEnum<TEnum>(this string strEnumValue, TEnum defaultValue)
		{
			if (!Enum.IsDefined(typeof(TEnum), strEnumValue)) {
				return defaultValue;
			}

			return (TEnum)Enum.Parse(typeof(TEnum), strEnumValue);
		}
	}
}

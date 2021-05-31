using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TizenSensor.lib
{
	public static class Util
	{
		public static string FormatTime(int seconds)
		{
			return $"{seconds / 60:0}:{seconds % 60:00}";
		}

		public static string GetFormattedDateTime()
		{
			return DateTime.Now.ToString("yy-MM-dd-HH-mm-ss");
		}
	}
}

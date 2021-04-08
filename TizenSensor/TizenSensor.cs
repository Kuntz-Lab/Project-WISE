using System;

using Tizen.Wearable.CircularUI.Forms;

using Xamarin.Forms;

namespace TizenSensor
{
	class Program : global::Xamarin.Forms.Platform.Tizen.FormsApplication
	{
		protected override void OnCreate()
		{
			base.OnCreate();

			LoadApplication(new App());
		}

		static void Main(string[] args)
		{
			var app = new Program();
			Forms.Init(app);
			FormsCircularUI.Init();
			app.Run(args);
		}
	}
}

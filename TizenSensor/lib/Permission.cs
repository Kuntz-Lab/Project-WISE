using System;

using Tizen.Security;

namespace TizenSensor.lib
{
	/// <summary>
	/// Provides functions to check or ask the user for certain permissions.
	/// </summary>
	public class Permission
	{
		public static void Check(Action<bool> callback, params string[] permissionStrings)
		{
			int numPending = permissionStrings.Length;
			bool areAllAllowed = true;
			foreach (string permissionString in permissionStrings)
			{
				Check(permissionString, isAllowed =>
				{
					areAllAllowed &= isAllowed;
					numPending--;
					if (numPending == 0) callback(areAllAllowed);
				});
			}
		}

		protected static void Check(string permissionString, Action<bool> callback)
		{
			var initPermission = PrivacyPrivilegeManager.CheckPermission(permissionString);
			if (initPermission == CheckResult.Allow)
			{
				callback(true);
				Console.WriteLine("Locator.cs **perm** allowed " + permissionString);
				return;
			}
			if (initPermission == CheckResult.Deny)
			{
				Console.WriteLine("Locator.cs **perm** denied " + permissionString);
				callback(false);
				return;
			}
			if (!PrivacyPrivilegeManager.GetResponseContext(permissionString).TryGetTarget(out var context))
			{
				Console.WriteLine("Locator.cs **perm** said no to " + permissionString);
				callback(false);
				return;
			}
			context.ResponseFetched += (sender, e) => callback(e.result == RequestResult.AllowForever);
			PrivacyPrivilegeManager.RequestPermission(permissionString);
		}
	}
}

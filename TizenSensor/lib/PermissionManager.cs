using System;

using Tizen.Security;

namespace TizenSensor.lib
{
	/// <summary>
	/// Provides functions to check or ask the user for certain permissions.
	/// </summary>
	public class PermissionManager
	{
		public static void GetPermissions(Action<bool> onGotPermissions, params string[] permissions)
		{
			int numPending = permissions.Length;
			bool areAllAllowed = true;
			foreach (string permissionString in permissions)
			{
				Check(permissionString, isAllowed =>
				{
					areAllAllowed &= isAllowed;
					numPending--;
					if (numPending == 0) onGotPermissions(areAllAllowed);
				});
			}
		}

		protected static void Check(string permission, Action<bool> callback)
		{
			var initPermission = PrivacyPrivilegeManager.CheckPermission(permission);
			if (initPermission == CheckResult.Allow)
			{
				callback(true);
				return;
			}
			if (initPermission == CheckResult.Deny)
			{
				callback(false);
				return;
			}
			if (!PrivacyPrivilegeManager.GetResponseContext(permission).TryGetTarget(out var context))
			{
				callback(false);
				return;
			}
			context.ResponseFetched += (sender, e) => callback(e.result == RequestResult.AllowForever);
			PrivacyPrivilegeManager.RequestPermission(permission);
		}
	}
}

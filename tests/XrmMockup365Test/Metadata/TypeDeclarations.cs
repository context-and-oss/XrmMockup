using System;

namespace DG.Tools.XrmMockup {
	public struct SecurityRoles {
		public static Guid SystemAdministrator = new Guid("edfa07f1-a1ba-f011-bbd3-7c1e52365f30");
		public static Guid XrmMockupTestAccessTeam = new Guid("96e87cec-a568-f111-ab0e-70a8a52b6964");
		public static Guid XrmMockupTestNoContactAccess = new Guid("ca9e5b10-5766-f111-ab0c-70a8a52b6964");
		public static Guid XrmMockupTestReadOnly = new Guid("279f5b10-5766-f111-ab0c-70a8a52b6964");
		public static Guid XrmMockupTestUser = new Guid("6798a312-5666-f111-ab0c-70a8a52b6964");
	}
}

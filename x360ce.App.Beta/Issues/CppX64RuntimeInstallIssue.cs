using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.IssuesControl;
using System;

namespace x360ce.App.Issues
{
    public class CppX64RuntimeInstallIssue : IssueItem
	{

		public CppX64RuntimeInstallIssue() : base()
		{
			Name = "Software";
			FixName = "Open Microsoft Download";
			MoreInfo = new Uri("https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist");
		}

		const string RuntimeName = "Microsoft Visual C++ v14 Redistributable (x64)";
		string lastLoggedDetection;
		public CppRuntimeDetectionResult DetectionResult { get; private set; }

		public override void CheckTask()
		{
			DetectionResult = new CppRuntimeDetector().Detect(CppRuntimeArchitecture.X64);
			var signature = $"{DetectionResult.IsInstalled}|{DetectionResult.Version}|{DetectionResult.RegistryView}|{DetectionResult.ErrorMessage}";
			if (!string.Equals(signature, lastLoggedDetection, StringComparison.Ordinal))
				x360ce.App.Diagnostics.OperationalLog.Current?.Write("cpp_runtime_detected", fields:
				new System.Collections.Generic.Dictionary<string, object>
				{
					["architecture"] = "x64",
					["installed"] = DetectionResult.IsInstalled,
					["version"] = DetectionResult.Version,
					["registryView"] = DetectionResult.RegistryView,
					["error"] = DetectionResult.ErrorMessage,
				});
			lastLoggedDetection = signature;
			if (!DetectionResult.IsApplicable)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			if (!DetectionResult.IsInstalled)
			{
				SetSeverity(
					IssueSeverity.Moderate, 1,
					"Install " + RuntimeName
				);
				return;
			}
			SetSeverity(IssueSeverity.None);
		}

		public override void FixTask()
		{
			x360ce.App.Diagnostics.OperationalLog.Current?.Write("cpp_runtime_download_opened", fields:
				new System.Collections.Generic.Dictionary<string, object> { ["architecture"] = "x64" });
			ControlsHelper.OpenUrl("https://aka.ms/vc14/vc_redist.x64.exe");
		}
    }
}

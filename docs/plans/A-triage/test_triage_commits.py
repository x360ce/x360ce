"""Unit tests for the pure classification half of triage_commits.py."""
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from triage_commits import (
    classify_file, normalize_numstat_path,
    compute_buckets, compute_flags, compute_risk,
)


class TestClassifyFile(unittest.TestCase):
    def test_engine_data(self):
        self.assertEqual(classify_file("x360ce.Engine/Data/UserSetting.cs"), "engine_data")
        self.assertEqual(classify_file("x360ce.Engine/x360ceModel.edmx"), "engine_data")
        self.assertEqual(classify_file("x360ce.Engine/IWebService.cs"), "engine_data")
        self.assertEqual(classify_file("x360ce.Data/Tables/dbo.Products.sql"), "engine_data")

    def test_web_services(self):
        self.assertEqual(classify_file("x360ce.Web/WebServices/x360ce.asmx.cs"), "web_services")
        self.assertEqual(classify_file("x360ce.Web/App_Code/Helper.cs"), "web_services")

    def test_engine_and_web_other(self):
        self.assertEqual(classify_file("x360ce.Engine/Engine.cs"), "engine")
        self.assertEqual(classify_file("x360ce.Web/Default.aspx.cs"), "web_other")

    def test_app_buckets(self):
        self.assertEqual(classify_file("x360ce.App.4/MainWindow.xaml.cs"), "app_v4")
        self.assertEqual(classify_file("x360ce.App/MainForm.cs"), "app_v3")
        self.assertEqual(classify_file("x360ce.App.Beta/Program.cs"), "app_v3")
        self.assertEqual(classify_file("x360ce.App.WPF/App.xaml"), "app_v3")

    def test_native(self):
        self.assertEqual(classify_file("x360ce/x360ce/Config.cpp"), "native")
        self.assertEqual(classify_file("MinHook/src/hook.c"), "native")
        self.assertEqual(classify_file("x360ce.RemoteController/main.cpp"), "native")
        self.assertEqual(classify_file("SomeDir/thing.vcxproj.filters"), "native")

    def test_build(self):
        self.assertEqual(classify_file("x360ce.sln"), "build")
        self.assertEqual(classify_file("Build_All.cmd"), "build")
        self.assertEqual(classify_file(".gitignore"), "build")

    def test_docs(self):
        self.assertEqual(classify_file("README.MD"), "docs")
        self.assertEqual(classify_file("Documents/Help.txt"), "docs")
        self.assertEqual(classify_file("banner.png"), "docs")

    def test_other_fallback(self):
        self.assertEqual(classify_file("SomeDir/data.bin"), "other")

    def test_first_match_wins(self):
        # csproj under x360ce.Engine is engine (rule 3) before build (rule 8)
        self.assertEqual(classify_file("x360ce.Engine/x360ce.Engine.csproj"), "engine")
        # ps1 under Documents is build (rule 8) before docs (rule 9)
        self.assertEqual(classify_file("Documents/Install.ps1"), "build")
        # nested image is NOT docs (docs images are root-level only) -> other
        self.assertEqual(classify_file("SomeDir/img.png"), "other")


class TestNormalizeNumstatPath(unittest.TestCase):
    def test_plain(self):
        self.assertEqual(normalize_numstat_path("a/b/c.cs"), "a/b/c.cs")

    def test_whole_rename(self):
        self.assertEqual(normalize_numstat_path("old.cs => new.cs"), "new.cs")

    def test_brace_rename(self):
        self.assertEqual(
            normalize_numstat_path("x360ce.App/{Forms => Controls}/Pad.cs"),
            "x360ce.App/Controls/Pad.cs")

    def test_brace_rename_empty_side(self):
        self.assertEqual(
            normalize_numstat_path("x360ce.App/{ => Sub}/Pad.cs"),
            "x360ce.App/Sub/Pad.cs")


class TestComputeBuckets(unittest.TestCase):
    def test_aggregation_and_loc(self):
        files = [("x360ce.App/MainForm.cs", 10, 2), ("README.MD", 3, 1)]
        buckets, loc, xaml = compute_buckets(files)
        self.assertTrue(buckets["app_v3"])
        self.assertTrue(buckets["docs"])
        self.assertFalse(buckets["engine"])
        self.assertEqual(loc["app_v3"], {"ins": 10, "del": 2})
        self.assertEqual(loc["docs"], {"ins": 3, "del": 1})
        self.assertFalse(xaml)

    def test_app_ui_xaml_overlay(self):
        files = [("x360ce.App.4/MainWindow.xaml", 5, 5)]
        _, _, xaml = compute_buckets(files)
        self.assertTrue(xaml)

    def test_xaml_outside_app_not_overlay(self):
        files = [("x360ce.Engine/Themes/Generic.xaml", 5, 5)]
        _, _, xaml = compute_buckets(files)
        self.assertFalse(xaml)


class TestComputeFlags(unittest.TestCase):
    def _flags(self, paths, parents=1):
        files = [(p, 1, 1) for p in paths]
        buckets, _, _ = compute_buckets(files)
        return compute_flags(files, buckets, parents)

    def test_version_bump(self):
        f = self._flags(["x360ce.App/Properties/AssemblyInfo.cs", "Version.cs"])
        self.assertTrue(f["is_version_bump"])
        f = self._flags(["x360ce.App/Properties/AssemblyInfo.cs", "x360ce.App/MainForm.cs"])
        self.assertFalse(f["is_version_bump"])

    def test_docs_only(self):
        self.assertTrue(self._flags(["README.MD", "Documents/a.txt"])["is_docs_only"])
        self.assertFalse(self._flags(["README.MD", "x360ce.sln"])["is_docs_only"])

    def test_merge_no_changes(self):
        f = compute_flags([], compute_buckets([])[0], 2)
        self.assertTrue(f["is_merge_no_changes"])
        f = compute_flags([], compute_buckets([])[0], 1)
        self.assertFalse(f["is_merge_no_changes"])

    def test_touches_data_model(self):
        self.assertTrue(self._flags(["x360ce.Engine/Data/UserSetting.cs"])["touches_data_model"])
        self.assertTrue(self._flags(["SomeDir/Model.edmx.diagram"])["touches_data_model"])
        self.assertFalse(self._flags(["x360ce.App/MainForm.cs"])["touches_data_model"])

    def test_touches_settings(self):
        for p in ["x360ce.App/SettingsManager.cs", "x360ce.Engine/JocysCom/Options.cs",
                  "x360ce.Engine/Data/PadSetting.cs", "x360ce.App/UserGameControl.cs",
                  "x360ce.App/PresetForm.cs"]:
            self.assertTrue(self._flags([p])["touches_settings"], p)
        self.assertFalse(self._flags(["x360ce.App/MainForm.cs"])["touches_settings"])

    def test_touches_webservice_api(self):
        self.assertTrue(self._flags(["x360ce.Web/WebServices/x360ce.asmx.cs"])["touches_webservice_api"])
        self.assertTrue(self._flags(["x360ce.Engine/IWebService.cs"])["touches_webservice_api"])
        self.assertFalse(self._flags(["x360ce.Web/Default.aspx"])["touches_webservice_api"])


class TestComputeRisk(unittest.TestCase):
    def _risk(self, paths, parents=1):
        files = [(p, 1, 1) for p in paths]
        buckets, _, _ = compute_buckets(files)
        flags = compute_flags(files, buckets, parents)
        return compute_risk(buckets, flags, files)

    def test_skip_wins_over_high(self):
        level, _ = self._risk(["README.MD"])
        self.assertEqual(level, "SKIP")

    def test_high_engine_data(self):
        level, reason = self._risk(["x360ce.Engine/Data/UserSetting.cs"])
        self.assertEqual(level, "HIGH")
        self.assertIn("engine_data", reason)

    def test_high_by_filename_in_low_bucket(self):
        # *Setting*.cs inside app bucket still HIGH per design §6 row 2
        level, _ = self._risk(["x360ce.App/SettingsDatabaseForm.cs"])
        self.assertEqual(level, "HIGH")

    def test_medium(self):
        level, _ = self._risk(["x360ce.Engine/Common.cs"])
        self.assertEqual(level, "MEDIUM")

    def test_low(self):
        level, _ = self._risk(["x360ce.App/MainForm.cs", "x360ce.sln"])
        self.assertEqual(level, "LOW")

    def test_skip_merge_no_changes(self):
        level, _ = self._risk([], parents=2)
        self.assertEqual(level, "SKIP")


if __name__ == "__main__":
    unittest.main()

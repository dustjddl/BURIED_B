using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.TestTools;

namespace CoffinEscape.Tests
{
    public static class EditModeTestRunner
    {
        private const string ResultKey = "CoffinEscape.M1EditModeTests.Result";
        private static TestRunnerApi activeRunner;
        private static RunCallbacks activeCallbacks;

        [MenuItem("CoffinEscape/Run M1 EditMode Tests")]
        public static void Run()
        {
            if (activeRunner != null)
                return;

            EditorPrefs.SetString(ResultKey, "RUNNING");
            WriteResult("RUNNING");
            activeRunner = ScriptableObject.CreateInstance<TestRunnerApi>();
            activeCallbacks = new RunCallbacks();
            activeRunner.RegisterCallbacks(activeCallbacks);
            activeRunner.Execute(new ExecutionSettings(new Filter
            {
                testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode,
                assemblyNames = new[] { "CoffinEscape.EditModeTests" }
            }));
        }

        private sealed class RunCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                string summary = $"{result.ResultState}: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped";
                AppendFailures(result, ref summary);
                EditorPrefs.SetString(ResultKey, summary);
                WriteResult(summary);
                Debug.Log($"CoffinEscape M1 EditMode tests: {summary}");
                activeRunner.UnregisterCallbacks(activeCallbacks);
                Object.DestroyImmediate(activeRunner);
                activeRunner = null;
                activeCallbacks = null;
            }
        }

        private static void WriteResult(string result)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string resultPath = Path.Combine(projectRoot, "Library", "CoffinEscape.M1EditModeTests.Result.txt");
            File.WriteAllText(resultPath, result);
        }

        private static void AppendFailures(ITestResultAdaptor node, ref string summary)
        {
            foreach (ITestResultAdaptor child in node.Children)
            {
                if (child.TestStatus == TestStatus.Failed)
                    summary += $"\n{child.FullName}: {child.Message}";
                AppendFailures(child, ref summary);
            }
        }
    }
}

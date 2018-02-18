﻿using System;
using System.Linq;
using System.Text;
using System.Windows;

namespace Kekiri.TestGen
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnGenerateClassicClick(object sender, RoutedEventArgs e)
        {
            ProcessScenario(ScenarioStyle.Classic);
        }

        void ProcessScenario(ScenarioStyle style)
        {
            var builder = new StringBuilder();

            var stepType = StepType.Given;

            foreach (var line in _textBox.Text.Split(Environment.NewLine.ToCharArray())
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                ProcessScenarioLine(style, line, ref stepType, builder);
            }

            // remove the extra whitespace line after methods
            builder.Remove(builder.Length - 2, 2);

            builder.AppendLine("   }");

            var scenario = builder.ToString();

            Clipboard.SetText(scenario);
        }

        private void ProcessScenarioLine(ScenarioStyle style, string line, ref StepType stepType, StringBuilder builder)
        {
            if (stepType == StepType.When)
            {
                stepType = StepType.Then;
            }

            if (line.StartsWith("When"))
            {
                stepType = StepType.When;
            }

            const string tagToken = "@";
            if (line.StartsWith(tagToken))
            {
                var tags = line.Split('@');

                foreach (var tag in tags
                    .Select(t => t.Trim().Trim('@'))
                    .Where(t => !string.IsNullOrWhiteSpace(t)))
                {
                    builder.AppendLine($"   [Tag(\"{tag}\")]");
                }
                return;
            }
            const string scenarioToken = "Scenario:";
            if (line.StartsWith(scenarioToken))
            {
                if (style == ScenarioStyle.Classic) builder.AppendLine("   [Scenario(Feature.Unknown)]");
                string className = Sanitize(line.Substring(scenarioToken.Length));
                builder.AppendLine($"   public class {className} : {GetScenarioTypeName(style)}");
                builder.AppendLine("   {");

                return;
            }

            if (line.StartsWith("-> done: "))
            {
                return;
            }

            var methodName = ProcessStepLine(line, stepType);

            if (style == ScenarioStyle.Classic) builder.AppendLine($"      [{stepType}]");
            builder.AppendLine($"      {GetAccessModifierForStep(style)}void {methodName}()");
            builder.AppendLine("      {");
            builder.AppendLine("      }");
            builder.AppendLine();
        }

        static string GetAccessModifierForStep(ScenarioStyle style)
        {
            switch (style)
            {
                case ScenarioStyle.Classic:
                    return "public ";
                default:
                    throw new NotSupportedException($"{style} is not supported");
            }
        }

        static string GetScenarioTypeName(ScenarioStyle style)
        {
            switch (style)
            {
                case ScenarioStyle.Classic:
                    return "Test";
                default:
                    throw new NotSupportedException($"{style} is not supported");
            }
        }

        private string ProcessStepLine(string stepLine, StepType stepType)
        {
            string str = stepLine;

            var stepToken = stepType.ToString();
            
            const string andToken = "And";
            if (str.StartsWith(andToken, StringComparison.OrdinalIgnoreCase))
            {
                str = str.Substring(andToken.Length);
                stepToken = andToken;
            }

            const string butToken = "But";
            if (str.StartsWith(butToken, StringComparison.OrdinalIgnoreCase))
            {
                str = str.Substring(butToken.Length);
                stepToken = butToken;
            }

            if (str.StartsWith(stepToken, StringComparison.OrdinalIgnoreCase))
            {
                str = str.Substring(stepToken.Length);
            }

            str = Sanitize(str);

            return string.Format("{0}_{1}", stepToken, str);
        }

        private string Sanitize(string str)
        {
            return str
                .Trim()
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace("\"", "")
                .Replace("’", "")
                .Replace("'", "")
                .Replace("“", "")
                .Replace("”", "");
        }
    }
}

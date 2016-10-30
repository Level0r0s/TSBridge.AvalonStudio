﻿using Jint;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TSBridge.LanguageService;
using TSBridge.Services;

namespace TSBridge
{
    public class TypeScriptContext
    {
        private Engine _jsEngine;
        public LanguageServiceHost Host { get; set; }
        public CompilerOptions CompilerOptions { get; }

        public TypeScriptContext(CompilerOptions options)
        {
            _jsEngine = new Engine(x => { x.AllowClr(typeof(TypeScriptContext).Assembly); });
            Host = new LanguageServiceHost(options, new NullLogger());
        }

        public TypeScriptContext() : this(new CompilerOptions())
        {
        }

        public void LoadComponents()
        {
            // Load the compiler services
            var compilerSourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TSBridge.JS.typescriptServices.js");
            var compilerSource = ReadStream(compilerSourceStream);
            _jsEngine.Execute(compilerSource);

            _jsEngine.SetValue("host", Host);
            _jsEngine.Execute("let ls = new ts.createLanguageService(host);");

            var libSourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TSBridge.TSDefs.lib.d.ts");
            var libSource = ReadStream(libSourceStream);

            Host.RegisterStdLib(libSource);

            //Host.OpenFile(Host.getDefaultLibFileName(CompilerOptions), libSource);
        }

        /// <summary>
        /// Gets the completions at a certain position
        /// </summary>
        /// <param name="fileName">The name of the loaded file</param>
        /// <param name="position">The position in the string to complete at</param>
        /// <returns>Returns a CompletionInfo if any completions were available. If no completions were available, returns null.</returns>
        public CompletionInfo GetCompletionsAtPosition(string fileName, int position)
        {
            var completionsJsonNative = _jsEngine.Execute($"JSON.stringify(ls.getCompletionsAtPosition('{Regex.Escape(fileName)}', {position}))")
                .GetCompletionValue();
            if (completionsJsonNative.IsUndefined())
            {
                //No completions?
                return null;
            }
            var completionsJsonString = completionsJsonNative.AsString();
            var completionInfo = JsonConvert.DeserializeObject<CompletionInfo>(completionsJsonString);
            return completionInfo;
        }

        public async Task LoadComponentsAsync()
        {
            await Task.Run(() => LoadComponents());
        }

        private string ReadStream(Stream stream)
        {
            var value = string.Empty;
            using (stream)
            {
                using (var sourceReader = new StreamReader(stream))
                {
                    value = sourceReader.ReadToEnd();
                }
            }
            return value;
        }

        public void OpenFile(string filename, string text)
        {
            Host.OpenFile(filename, text);
            //create AST from file

            //var ls = _jsEngine.GetValue("ls");

            //string emitOutput = _jsEngine
            //    .Execute("JSON.stringify(ls.getEmitOutput('" + filename.Replace("\\", "\\\\") + "'), null, 4)")
            //    .GetCompletionValue()
            //    .ToObject() as string;
            //JObject treeObj = JObject.Parse(tree);
            //this.SyntaxTree.Clear();
            //this.SyntaxTree.Add(new AstTreeItem(treeObj["sourceUnit"]));
        }

        public string BuildAstJson(string filename, string text)
        {
            //var createSourceFile = _jsEngine.GetValue("ts.createSourceFile");
            //var sourceFileAst = createSourceFile.Invoke(filename, text);
            //var outputJson = JintJson.Stringify(sourceFileAst);

            string outputJson = _jsEngine
                .Execute($"JSON.stringify(ts.createSourceFile('{Regex.Escape(filename)}', '{Regex.Escape(text)}'), null, 4)")
                .GetCompletionValue()
                .ToObject() as string;
            return outputJson;
        }

        public void RemoveFile(string filename)
        {
            Host.RemoveFile(filename);
        }
    }
}
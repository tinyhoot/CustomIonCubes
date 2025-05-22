using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nautilus.Utility.ModMessages;

namespace CustomIonCubes
{
    internal class CustomCubeMessageReader : ModMessageReader
    {
        private readonly string _subject;
        private List<MethodInfo> _targetMethods;
        
        public CustomCubeMessageReader(string subject)
        {
            _subject = subject;
            // Get all overloads of the cube registering method.
            _targetMethods = AccessTools.GetDeclaredMethods(typeof(CustomCubeHandler))
                .Where(method => method.Name == nameof(CustomCubeHandler.RegisterCube))
                .ToList();
        }

        protected override void OnReceiveMessage(ModMessage message)
        {
            if (message.Subject != _subject)
                CustomIonCubesInit._log.LogWarning($"Unexpected mod message subject '{message.Subject}', " +
                                                   $"attempting to parse anyway...");

            ParseModMessage(message.Contents);
        }

        protected override bool TryHandleDataRequest(ModMessage message, out object returnValue)
        {
            if (message.Subject != _subject)
                CustomIonCubesInit._log.LogWarning($"Unexpected mod message subject '{message.Subject}', " +
                                                   $"attempting to parse anyway...");

            returnValue = ParseModMessage(message.Contents);
            return true;
        }
        
        private TechType ParseModMessage(object[] args)
        {
            var targetMethod = _targetMethods.FirstOrDefault(method => method.GetParameters().Length == args.Length);
            if (targetMethod is null)
            {
                CustomIonCubesInit._log.LogError($"Received mod message with invalid number of arguments: {args.Length}\n" +
                                                 $"{args}");
                return TechType.None;
            }

            TechType result = TechType.None;
            try
            {
                result = (TechType)targetMethod.Invoke(null, args);
            }
            catch (Exception ex)
            {
                CustomIonCubesInit._log.LogError("Failed to create custom ion cube through mod message system!\n" +
                                                 $"Error was: {ex.GetType()}: {ex.Message}\n{ex.StackTrace}");
            }

            return result;
        }
    }
}
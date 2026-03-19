using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace NoGoalTimer
{
	[BepInPlugin("superbattlegolf.nogoaltimer", "No Goal Timer", "1.0.0")]
	public class NoGoalTimerPlugin : BaseUnityPlugin
	{
		private Harmony _harmony;

		private void Awake()
		{
			_harmony = new Harmony("superbattlegolf.nogoaltimer");
			TryApplyPatch();
		}

		private void TryApplyPatch()
		{
			var fallbackTargets = new List<(string TypeName, string MethodName)>
			{
				("CourseManager", "BeginCountdownToMatchEnd"),
				("CourseManager", "CountDownToMatchEndRoutine")
			};

			foreach (var fallback in fallbackTargets)
			{
				var targetMethod = FindTargetMethod(fallback.TypeName, fallback.MethodName);
				if (targetMethod == null)
				{
					continue;
				}

				ApplyPrefixPatch(targetMethod, $"target {fallback.TypeName}.{fallback.MethodName}");
				return;
			}

			Logger.LogError(
				"Patch target not found. This game build may have changed the CourseManager countdown method names.");
		}

		private void ApplyPrefixPatch(MethodInfo targetMethod, string source)
		{
			if (targetMethod == null)
			{
				return;
			}

			var prefix = new HarmonyMethod(AccessTools.Method(typeof(NoGoalTimerPlugin), nameof(BlockCountdownPrefix)));
			_harmony.Patch(targetMethod, prefix: prefix);
			Logger.LogInfo($"Patched {targetMethod.DeclaringType?.FullName}.{targetMethod.Name} using {source}. Post-goal timer is disabled.");
		}

		private static MethodInfo FindTargetMethod(string typeName, string methodName)
		{
			var type = AccessTools.TypeByName(typeName);
			if (type != null)
			{
				return AccessTools.DeclaredMethod(type, methodName);
			}

			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				Type[] types;
				try
				{
					types = assembly.GetTypes();
				}
				catch
				{
					continue;
				}

				foreach (var candidate in types)
				{
					if (!string.Equals(candidate.Name, typeName, StringComparison.Ordinal))
					{
						continue;
					}

					var method = AccessTools.DeclaredMethod(candidate, methodName);
					if (method != null)
					{
						return method;
					}
				}
			}

			return null;
		}

		private static bool BlockCountdownPrefix()
		{
			return false;
		}
	}
}

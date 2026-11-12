using System.Collections.Generic;

public class ScriptManager
{
	private List<Script> scripts = new List<Script>();

	private List<Script> runningScripts = new List<Script>();

	public void AddScript(Script script)
	{
		scripts.Add(script);
	}

	public void RunScript(string scriptName, bool skip)
	{
		foreach (Script script in scripts)
		{
			if (script.GetName() == scriptName)
			{
				runningScripts.Add(script);
				if (skip)
				{
					script.SkipSequence();
				}
				break;
			}
		}
	}

	public void RunScriptNumber(int flag)
	{
		string text = "trigger" + flag;
		foreach (Script script in scripts)
		{
			if (script.GetName() == text)
			{
				runningScripts.Add(script);
				break;
			}
		}
	}

	public void Update()
	{
		for (int i = 0; i < runningScripts.Count; i++)
		{
			runningScripts[i].Update();
		}
	}

	public string DumpScripts()
	{
		string text = "Scripts: ";
		foreach (Script script in scripts)
		{
			text = text + "\n" + script.ToString();
		}
		return text;
	}

	public List<string> GetMusic()
	{
		List<string> list = new List<string>();
		foreach (Script script in scripts)
		{
			list.AddRange(script.GetMusic());
		}
		return list;
	}

	public string GetIntroMusic()
	{
		foreach (Script script in scripts)
		{
			if (script.GetName() != "intro") continue;
			List<string> music = script.GetMusic();
			return music.Count > 0 ? music[0] : null;
		}
		return null;
	}

	public List<T> GetTasks<T>() where T : class
	{
		List<T> list = new List<T>();
		foreach (Script script in scripts)
		{
			list.AddRange(script.GetTasks<T>());
		}
		return list;
	}
}

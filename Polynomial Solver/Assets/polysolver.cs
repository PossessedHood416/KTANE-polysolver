//if youre reading this pls tell me how to fix my mod (if i should) and also general optimizations :)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using Math = ExMath;

public class polysolver : MonoBehaviour {

	public KMBombInfo Bomb;
	public KMAudio Audio;

	public KMSelectable[] Button8;
	public KMSelectable[] ButtonS;
	public KMSelectable ButtonR;
	public KMSelectable ButtonE;
	public TextMesh DisplayText;

	int Xvalue;
	private bool ZenModeActive;
	int Zen;
	string[] SolveTxt	= {"UH-HUH","RIGHT!","[COOL]","YIPPEE","[OKAY]","GUDJOB","YESSIR","GOT IT","GAMER!","SOLVED","!!!!!!","RADGE."," YES. ","GREAT!","[EPIC]","NICEON","YOUWIN"};
	string[] StrikeTxt	= {"NUH-UH","WRONG!","[LMAO]","DCOLON","[BRUH]","GITGUD","[NOPE]","FALSE!","STUPID","STRIKE","??????","SADGE.","OH NO.","HUH?!?","GUH!?!","LAME-O","PRESSF"};
	private List<int> Polynomial = new List<int>();
	private List<int> Submission = new List<int>();
	private int Stage;
	private string Sign;
	private int Cooldown;
	static int ModuleIdCounter = 1;
	int ModuleId;
	private bool ModuleSolved = false;

	void Awake () { //Avoid doing calculations in here regarding edgework. Just use this for setting up buttons for simplicity.
		ModuleId = ModuleIdCounter++;
		GetComponent<KMBombModule>().OnActivate += Activate;
		
		foreach (KMSelectable Key in Button8) Key.OnInteract += delegate () { Button8Press(Key); return false; };
		foreach (KMSelectable Key in ButtonS) Key.OnInteract += delegate () { ButtonSPress(Key); return false; };
		
		ButtonR.OnInteract += delegate () { ButtonRPress(); return false; };
		ButtonE.OnInteract += delegate () { ButtonEPress(); return false; };
	}

	void Button8Press (KMSelectable Bun){	
		Bun.AddInteractionPunch();
		if(ModuleSolved) return;

		int i = 0;
		for(; i < 8; i++) if(Bun == Button8[i]) break;

		if(Submission.Count == Polynomial.Count){
			//the fuckin fnaf1 Light Not Working sfx :D
			Audio.PlaySoundAtTransform("Error", Bun.transform);
			return;
		}

		Submission.Add(i * (Sign == "+" ? 1 : -1));
		Audio.PlaySoundAtTransform("ButtonPress" + (Submission.Count).ToString(), Bun.transform);
		Debug.LogFormat("[Polynomial Solver #{0}] Pressed {1}{2}", ModuleId, Sign, i);
	}

	void ButtonSPress (KMSelectable Bun){
		Bun.AddInteractionPunch();
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Bun.transform);

		if(ModuleSolved) return;
		Sign = (Bun == ButtonS[0] ? "+" : "-");
	}

	void ButtonRPress (){
		ButtonR.AddInteractionPunch();
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ButtonR.transform);
		if(ModuleSolved) return;

		Submission.Clear();
		Debug.LogFormat("[Polynomial Solver #{0}] Input reset.", ModuleId);
	}

	void ButtonEPress (){
		ButtonE.AddInteractionPunch();
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ButtonE.transform);
		if(ModuleSolved) return;

		Debug.LogFormat("[Polynomial Solver #{0}] Submited: {1}", ModuleId, String.Join(", ", Submission.Select(x => x.ToString()).ToArray()) );
		Debug.LogFormat("[Polynomial Solver #{0}] Answer: {1}", ModuleId, String.Join(", ", Polynomial.Select(x => x.ToString()).ToArray()) );

		if(!Polynomial.SequenceEqual(Submission)){
			Debug.LogFormat("[Polynomial Solver #{0}] Wrong!", ModuleId);
			Strike();
			Submission.Clear();
			return;
		}

		Debug.LogFormat("[Polynomial Solver #{0}] Right!", ModuleId);
		Audio.PlaySoundAtTransform("InputCorrect", ButtonE.transform);
		Submission.Clear();	

		if (Stage >= 5) Solve();
		else {
			Stage++;
			Cooldown = (int)Bomb.GetTime()-(2*Zen);
			DisplayText.text = ("STAGE" + Stage.ToString());
			MakePoly(Stage);
		}
	}

	void Activate () { //Shit that should happen when the bomb arrives (factory)/Lights turn on
		Zen = (ZenModeActive ? -1 : 1);
		Debug.LogFormat("[Polynomial Solver #{0}] Detected timer ticking {1}.", ModuleId, (ZenModeActive ? "up" : "down"));

		DisplayText.text = "STAGE1";
		Sign = "+";
		Stage = 1;
		Cooldown = (int)Bomb.GetTime()-(2*Zen);

		MakePoly(Stage);
	}

	void Update () {
		if(ModuleSolved) return;
		
		Xvalue = Modulo((int)(Bomb.GetTime()*-1*Zen), (Stage + 2));

		if(Cooldown*Zen > Bomb.GetTime()*Zen)
			DisplayText.text = (Xvalue == 0 ? "" : (Calc(Xvalue)).ToString());

		else if(Cooldown - 5 > Bomb.GetTime())
			Cooldown = (int)Bomb.GetTime()+2;

		else if(Cooldown + 5 < Bomb.GetTime())
			Cooldown = (int)Bomb.GetTime()-2;
	}
	
	void Solve () {
		GetComponent<KMBombModule>().HandlePass();
		ModuleSolved = true;
		Cooldown = (int)Bomb.GetTime()-(2*Zen);
		DisplayText.text = SolveTxt[Rnd.Range(0,SolveTxt.Length)];
	}

	void Strike () {
		GetComponent<KMBombModule>().HandleStrike();
		Cooldown = (int)Bomb.GetTime()-(2*Zen);
		DisplayText.text = StrikeTxt[Rnd.Range(0,StrikeTxt.Length)];
	}

	void MakePoly (int Stage) {
		Debug.LogFormat("[Polynomial Solver #{0}] Generating polynomial for stage {1} (degree {1}).", ModuleId, Stage);
		Polynomial.Clear();
	
		for(int i = 0; i<= Stage; i++) Polynomial.Add(Rnd.Range(-7, 8));
		while(Polynomial[0] == 0) Polynomial[0] = Rnd.Range(-7, 8);

		Debug.LogFormat("[Polynomial Solver #{0}] Coefficients: {1}", ModuleId, String.Join(", ", Polynomial.Select(x => x.ToString()).ToArray()) );
	}

	int Calc (int x){
		int Total = 0;
		for (int i = 0; i<= Stage; i++)	Total += Pow(x, Stage - i) * Polynomial[i];
		return Total;
	}

	int Pow (int x, int y){
		int Total = 1;
		for (int i = 0; i < y; i++)	Total *= x;
		return Total;
	}

	int Modulo (int num, int mod){
		return ((num % mod) + mod) % mod;
	}

//if youre reading this and the display isn't changing on tp training modes, then blame tp :P
#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"Use '!{0} + 0 1 2 3 - 4 5 6 7 E R' to press those buttons. Chain with spaces.";
#pragma warning restore 414

	IEnumerator ProcessTwitchCommand (string Command) {
		Command = Command.Trim().ToUpper();
		yield return null;
		
		string[] Commands = Command.Split(' ');

		for (int i = 0; i < Commands.Length; i++) {
			if (Commands[i].Length != 1) {
				yield return "sendtochaterror Error: Commands can only be 1 character long, chain with spaces.";
				yield break;
			}
			if (!"01234567+-ER".Contains(Commands[i])) {
				yield return "sendtochaterror Error: Valid buttons are: 0 1 2 3 4 5 6 7 + - E R.";
				yield break;
			}
		}

		for (int i = 0; i < Commands.Length; i++) {
			if ("01234567".Contains(Commands[i])) {
				Button8[int.Parse(Commands[i])].OnInteract();

			} else if(Commands[i] == "+") {
				ButtonS[0].OnInteract();

			} else if(Commands[i] == "-") {
				ButtonS[1].OnInteract();
			
			} else if (Commands[i] == "R"){
				ButtonR.OnInteract();

			} else if (Commands[i] == "E"){
				ButtonE.OnInteract();

			}

			yield return new WaitForSeconds(.2f);
		}
	}

	IEnumerator TwitchHandleForcedSolve () {
		yield return null;
		Debug.LogFormat("[Polynomial Solver #{0}] TP autosolver activated, solving module.", ModuleId);
		
		while (!ModuleSolved){
			ButtonR.OnInteract();

			while(!Polynomial.SequenceEqual(Submission)){
				yield return new WaitForSeconds(0.3f);
				
				int i = Submission.Count;

				if(Polynomial[i] >= 0){
					ButtonS[0].OnInteract();
					Button8[Polynomial[i]].OnInteract();
				} else if (Polynomial[i] < 0){
					ButtonS[1].OnInteract();
					Button8[Polynomial[i]*-1].OnInteract();
				}
			}
			
			ButtonE.OnInteract();
		}
	}
}
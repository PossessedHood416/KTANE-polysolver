using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
//using Math = ExMath;

public class polysolver : MonoBehaviour {

	public KMBombInfo Bomb;
	public KMAudio Audio;

	public KMSelectable[] Button8;
	public KMSelectable[] ButtonS;
	public KMSelectable ButtonR;
	public KMSelectable ButtonE;
	public TextMesh DisplayText;

	public GameObject[] SignIndicator;
	public Material[] SignMat;

	private string[] SolveTxt = {"UH-HUH","RIGHT!","[COOL]","YIPPEE","[OKAY]","GUDJOB","YESSIR","GOT IT","GAMER!","SOLVED","!!!!!!","RADGE."," YES. ","GREAT!","[EPIC]","NICEON","YOUWIN"};
	private string[] StrikeTxt= {"NUH-UH","WRONG!","[LMAO]","DCOLON","[BRUH]","GITGUD","[NOPE]","FALSE!","STUPID","STRIKE","??????","SADGE.","OH NO.","HUH?!?","GUH!?!","LAME-O","PRESSF"};
	
	private int Zen;
	private bool ZenModeActive;
	
	private List<int> Polynomial = new List<int>();
	private List<int> Submission = new List<int>();
	
	//for Souvenir, [stage-1][displayIndex-1] = displayed num
	private int[][] Displays = new int[5][];
	
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
	}

	void ButtonSPress (KMSelectable Bun){
		Bun.AddInteractionPunch();
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Bun.transform);

		if(ModuleSolved) return;
		Sign = (Bun == ButtonS[0] ? "+" : "-");
		SignIndicator[0].GetComponent<Renderer>().material = SignMat[Sign == "+" ? 1 : 0];
		SignIndicator[1].GetComponent<Renderer>().material = SignMat[Sign == "-" ? 1 : 0];
	}

	void ButtonRPress (){
		ButtonR.AddInteractionPunch();
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ButtonR.transform);
		if(ModuleSolved) return;

		Submission.Clear();
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
		Debug.LogFormat("<Polynomial Solver #{0}> Detected timer ticking {1}.", ModuleId, (ZenModeActive ? "up" : "down"));

		DisplayText.text = "STAGE1";
		Sign = "+";
		Stage = 1;
		Cooldown = (int)Bomb.GetTime()-(2*Zen);

		MakePoly(Stage);
	}

	void Update () {
		if(ModuleSolved) return;
		
		int Xvalue = Modulo((int)(Bomb.GetTime()*-1*Zen), (Stage + 2));

		if(Cooldown*Zen > Bomb.GetTime()*Zen)
			DisplayText.text = (Xvalue == Stage+1 ? "" : Displays[Stage - 1][Xvalue].ToString());

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
		SignIndicator[0].GetComponent<Renderer>().material = SignMat[0];
		SignIndicator[1].GetComponent<Renderer>().material = SignMat[0];
	}

	void Strike () {
		GetComponent<KMBombModule>().HandleStrike();
		Cooldown = (int)Bomb.GetTime()-(2*Zen);
		DisplayText.text = StrikeTxt[Rnd.Range(0,StrikeTxt.Length)];
	}

	void MakePoly (int Stage) {
		Polynomial.Clear();
	
		for(int i = 0; i<= Stage; i++) Polynomial.Add(Rnd.Range(-7, 8));
		while(Polynomial[0] == 0) Polynomial[0] = Rnd.Range(-7, 8);

		Displays[Stage-1] = new int[Polynomial.Count];
		for (int i = 0; i < Stage + 1; i++) Displays[Stage - 1][i] = Calc(i+1);

		Debug.LogFormat("[Polynomial Solver #{0}] Generating polynomial for stage {1} (degree {1}).", ModuleId, Stage);
		Debug.LogFormat("[Polynomial Solver #{0}] Coefficients: {1}", ModuleId, String.Join(", ", Polynomial.Select(x => x.ToString()).ToArray()) );
		Debug.LogFormat("[Polynomial Solver #{0}] Display values: {1}", ModuleId, String.Join(", ", Displays[Stage - 1].Select(x => x.ToString()).ToArray()));
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
	private readonly string TwitchHelpMessage = @"Use '!{0} +01 23 - 456 7 ER' to press those buttons. Chain with or without spaces.";
#pragma warning restore 414

	IEnumerator ProcessTwitchCommand (string Command) {
		Command = Command.Trim().ToUpperInvariant().Replace(" ", "");
		char[] Commands = Command.ToCharArray();

		foreach (char cmd in Commands) {
			if (!"01234567+-ER".Contains(cmd)){
				yield return "sendtochaterror Error: Invalid command: " + cmd + ". No buttons pressed.";
				yield break;
			}
		}
		
		yield return null;

		foreach (char cmd in Commands) {
			switch (cmd) {
				case '0': case '1': case '2': case '3':
				case '4': case '5': case '6': case '7':
					Button8[cmd - '0'].OnInteract();
					break;
				case '+':
					ButtonS[0].OnInteract();
					break;
				case '-':
					ButtonS[1].OnInteract();
					break;
				case 'R':
					ButtonR.OnInteract();
					break;
				case 'E':
					ButtonE.OnInteract();
					break;
			}

			yield return new WaitForSeconds(.2f);
		}
	}

	IEnumerator TwitchHandleForcedSolve () {
		yield return null;
		Debug.LogFormat("[Polynomial Solver #{0}] TP autosolver activated, solving module.", ModuleId);
		
		while (!ModuleSolved){
			ButtonR.OnInteract();
			yield return new WaitForSeconds(0.2f);

			while(!Polynomial.SequenceEqual(Submission)){
				
				int i = Submission.Count;

				ButtonS[Polynomial[i] >= 0 ? 0 : 1].OnInteract();
				yield return new WaitForSeconds(0.2f);
			
				Button8[Math.Abs(Polynomial[i])].OnInteract();
				yield return new WaitForSeconds(0.2f);
			}
			
			ButtonE.OnInteract();
		}
	}
}
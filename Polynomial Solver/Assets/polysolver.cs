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

   //Bomb sound & info
   public KMBombInfo Bomb;
   public KMAudio Audio;

   //Buttons and shit   8=numbers   S=sign   R=reset  E=enter
   public KMSelectable[] Button8;
   public KMSelectable[] ButtonS;
   public KMSelectable ButtonR;
   public KMSelectable ButtonE;
   public TextMesh DisplayText;

   //vars
   //8 is a placeholder for no coeffecient
   int Xvalue;
   private bool ZenModeActive;
   int Zen;
   string[] SolveTxt  = {"UH-HUH","RIGHT!","[COOL]","YIPPEE","[OKAY]","GUDJOB","YESSIR","GOT IT","GAMER!","SOLVED","!!!!!!","RADGE."," YES. ","GREAT!","[EPIC]","NICEON","YOUWIN"};
   string[] StrikeTxt = {"NUH-UH","WRONG!","[LMAO]","DCOLON","[BRUH]","GITGUD","[NOPE]","FALSE!","STUPID","STRIKE","??????","SADGE.","OH NO.","HUH?!?","GUH!?!","LAME-O","PRESSF"};
   private int[] Polynomial = {8,8,8,8,8,8};
   private int[] Submission = {8,8,8,8,8,8};
   private int Stage;
   private string Sign;
   private int Cooldown;
   static int ModuleIdCounter = 1;
   int ModuleId;
   private bool ModuleSolved;

   void Awake () { //Avoid doing calculations in here regarding edgework. Just use this for setting up buttons for simplicity.
      ModuleId = ModuleIdCounter++;
      GetComponent<KMBombModule>().OnActivate += Activate;
      
      foreach (KMSelectable Key in Button8) {
          Key.OnInteract += delegate () { Button8Press(Key); return false; };
      }
      foreach (KMSelectable Key in ButtonS) {
          Key.OnInteract += delegate () { ButtonSPress(Key); return false; };
      }
      
      ButtonR.OnInteract += delegate () { ButtonRPress(); return false; };
      
      ButtonE.OnInteract += delegate () { ButtonEPress(); return false; };

   }

   void Button8Press (KMSelectable Bun){
      
      Bun.AddInteractionPunch();
      
      if(ModuleSolved){
         return;
      }
      
      for(int i = 0; i < 8; i++){
         if(Bun == Button8[i]){

            for(int j = 0; j <= 6; j++){

               if ((j > Stage && j!=8) || j == 8){
                  //the fuckin fnaf1 Light Not Working sfx :D
                  Audio.PlaySoundAtTransform("Error", Bun.transform);
                  break;
               }

               if(Submission[j] == 8){
                  
                  if(Sign == "+"){
                     Submission[j] = i;
                  } else{
                     Submission[j] = i * -1;
                  }
                  
                  //haha Sst sfx go brrrrr
                  Audio.PlaySoundAtTransform("ButtonPress" + (j+1).ToString(), Bun.transform);
                  Debug.LogFormat("[Polynomial Solver #{0}] Pressed {1}{2}", ModuleId, Sign, i);
                  
                  j = 8;
               
               }
            }

         }
      }
   }
   void ButtonSPress (KMSelectable Bun){

      Bun.AddInteractionPunch();
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Bun.transform);

      if(ModuleSolved){
         return;
      }

      if(Bun == ButtonS[0]){
         Sign = "+";
      } else if (Bun == ButtonS[1]){
         Sign = "-";
      }
   }
   void ButtonRPress (){

      ButtonR.AddInteractionPunch();
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ButtonR.transform);

      if(ModuleSolved){
         return;
      }

      for(int i = 0; i < 6; i++){
         Submission[i] = 8;
      }

      Debug.LogFormat("[Polynomial Solver #{0}] Input reset.", ModuleId);


   }
   void ButtonEPress (){

      ButtonE.AddInteractionPunch();
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ButtonE.transform);

      if(ModuleSolved){
         return;
      }

      bool Good = true;

      for(int i = 0; i < 6; i++){
         
         //how tf do i optimize this into smthn like a dropdown
         if(Polynomial[i] != 8){
            if(Submission[i] != 8){   
               Debug.LogFormat("[Polynomial Solver #{0}] Checking polynomial: {1} vs {2}", ModuleId, Submission[i], Polynomial[i]);
            } else {
               Debug.LogFormat("[Polynomial Solver #{0}] Error, too few coefficients were entered, striking :P", ModuleId);
            }
         }

         if(Submission[i] != Polynomial[i]){
            Good = false;
         }
      }

      if (Good){
         Debug.LogFormat("[Polynomial Solver #{0}] Right!", ModuleId);
         
         Audio.PlaySoundAtTransform("InputCorrect", ButtonE.transform);
         
         if (Stage >= 5){
            Solve();
         } else {
            Stage++;
            Cooldown = (int)Bomb.GetTime()-(2*Zen);
            DisplayText.text = ("STAGE" + Stage.ToString());
            MakePoly(Stage);
         }

      } else {
         Debug.LogFormat("[Polynomial Solver #{0}] Wrong!", ModuleId);
         Strike();
      }
      for(int i = 0; i < 6; i++){
         Submission[i] = 8;
      }
   }

   void OnDestroy () { //Shit you need to do when the bomb ends

   }

   void Activate () { //Shit that should happen when the bomb arrives (factory)/Lights turn on
      ModuleSolved = false;

      //zen kinda fucks up the displays bc of how they change w/ the timer. I'm 98% sure this fixes it with slightly more spaghetti code
      //also trainingmode hotfix (thanks vflyer tp stream)
      if (!ZenModeActive || Bomb.GetTime() < 1){
         Zen = 1;
      } else {
         Zen = -1;
      }
      
      DisplayText.text = "STAGE1";
      Sign = "+";
      Stage = 1;
      Cooldown = (int)Bomb.GetTime()-(2*Zen);

      MakePoly(Stage);
   }

   void Start () { //Shit that you calculate, usually a majority if not all of the module

   }

   void Update () { //Shit that happens at any point after initialization

      //Xvalue loops from 0 to (Stage+1) [inc] via bomb timer like fmzn
      Xvalue = Modulo((int)(Bomb.GetTime()*-1*Zen), (Stage + 2));

      //if mod strikes/solves, it waits to continue to a different display
      if(Cooldown*Zen > Bomb.GetTime()*Zen && !ModuleSolved){
         if (Xvalue == 0){
            DisplayText.text = "";
         } else {
            DisplayText.text = (Calc(Xvalue)).ToString();
         }
      } else if (Cooldown*Zen > Bomb.GetTime()*Zen && ModuleSolved){
         DisplayText.text = "";
      }

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
      
      Debug.LogFormat("[Polynomial Solver #{0}] Generating polynomial for stage {1} (degree {2}). Coefficiants are:", ModuleId, Stage, Stage+1);
      
      for(int i = 0; i<= Stage; i++){

         Polynomial[i] = Rnd.Range(-7, 7);
         
         //makes sure Leading Coeffeciant is not 0, bc that would lower the degree and make the mod easier, we dont want that to happen :) 
         while(Polynomial[0] == 0){
            Polynomial[i] = Rnd.Range(-7, 8);
         }

         //someone tell me how to optimize this, cuz it prints out the coeffs 1by1
         Debug.LogFormat("[Polynomial Solver #{0}] {1}", ModuleId, Polynomial[i]);

      }

   }

   int Calc (int x){

      int Total = 0;

      for (int i = 0; i<= Stage; i++){

         Total += Pow(x, Stage - i) * Polynomial[i];

      }

      return Total;

   }

   int Pow (int x, int y){

      int Total = 1;

      for (int i = 0; i < y; i++){
         Total *= x;
      }

      return Total;

   }

   int Modulo (int num, int mod){
      //stupid c# not having good modulo
      return ((num % mod) + mod) % mod;
   }

//i have little idea what im doing, lmk if i need to make changes
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

            } else
            if ("+-".Contains(Commands[i])){
               
               if(Commands[i] == "+"){
                  ButtonS[0].OnInteract();
               } else 
               if(Commands[i] == "-"){
                  ButtonS[1].OnInteract();
               }

            } else
            if (Commands[i] == "R"){

               ButtonR.OnInteract();

            } else
            if (Commands[i] == "E"){

               ButtonE.OnInteract();

            }

            //remember to change these back to smthn like (.2f) or smthn
            yield return new WaitForSeconds(.2f);

        }


   }

   IEnumerator TwitchHandleForcedSolve () {
      yield return null;
      
      while (!ModuleSolved)
      {
         ButtonR.OnInteract();

         for(int i = 0; i < 6; i++){

            yield return new WaitForSeconds(0.2f);

            if(Polynomial[i] == 8){
               break;
            }

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
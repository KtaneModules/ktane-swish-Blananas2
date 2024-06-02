using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class swishScript : MonoBehaviour
{

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable[] Controls;
    public GameObject ControlObj;
    public GameObject[] CardObjs;
    public SpriteRenderer[] SpriteSlots;
    public Sprite[] Sprites;

    int[][] cards = new int[][] { new int[] { -1, -1 }, new int[] { -1, -1 }, new int[] { -1, -1 }, new int[] { -1, -1 } };
    int[][] orientations = new int[][] {
        new int[] { 12, 8, 4, 0, 13, 9, 5, 1, 14, 10, 6, 2, 15, 11, 7, 3 }, //Rotate Clockwise
        new int[] { 3, 7, 11, 15, 2, 6, 10, 14, 1, 5, 9, 13, 0, 4, 8, 12 }, //Rotate Counter-clockwise
        new int[] { 12, 13, 14, 15, 8, 9, 10, 11, 4, 5, 6, 7, 0, 1, 2, 3 }, //Flip Vertical
        new int[] { 3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12 }, //Flip Horizontal
        new int[] { 0, 4, 8, 12, 1, 5, 9, 13, 2, 6, 10, 14, 3, 7, 11, 15 }, //Rotate Clockwise, Flip Horizontal
        new int[] { 15, 11, 7, 3, 14, 10, 6, 2, 13, 9, 5, 1, 12, 8, 4, 0 }, //Rotate Counter-clockwise, Flip Horizontal
        new int[] { 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 }, //Flip Vertical, Flip Horizontal
        new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }, //None
    };
    string[] solutionNames = { "Rotate Clockwise", "Rotate Counter-clockwise", "Flip Vertical", "Flip Horizontal", "Rotate Clockwise, Flip Horizontal", "Rotate Counter-clockwise, Flip Horizontal", "Flip Vertical, Flip Horizontal", "None" };
    string[] controlNames = { "Rotate Counter-clockwise", "Rotate Clockwise", "Flip Vertical", "Flip Horizontal" };
    string[] possibleSolution = { "", "", "", "" };
    string[] givenSolution = { "", "", "", "" };
    bool timerStarted = false;
    bool timerRanOut = false;
    bool animating = false;
    List<int> animq = new List<int> { };

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;

        foreach (KMSelectable Control in Controls)
        {
            Control.OnInteract += delegate () { ControlPress(Control); return false; };
        }
    }

    // Use this for initialization
    void Start()
    {
        GeneratePuzzle();
    }

    void GeneratePuzzle()
    {
        for (int z = 0; z < 4; z++)
        {
            do
            {
                cards[z][0] = Rnd.Range(0, 16);
            } while (cards[z][0] == cards[(z + 1) % 4][0] || cards[z][0] == cards[(z + 2) % 4][0] || cards[z][0] == cards[(z + 3) % 4][0]);
            cards[(z + 1) % 4][1] = cards[z][0];
        }
        cards.Shuffle();
        for (int z = 0; z < 4; z++)
        {
            int o = Rnd.Range(0, 8);
            cards[z][0] = orientations[o][cards[z][0]];
            cards[z][1] = orientations[o][cards[z][1]];
            possibleSolution[z] = solutionNames[o];
        }

        Debug.LogFormat("[Swish #{0}] Cards: {1},{2} {3},{4} {5},{6} {7},{8}", moduleId, cards[0][0], cards[0][1], cards[1][0], cards[1][1], cards[2][0], cards[2][1], cards[3][0], cards[3][1]);
        Debug.LogFormat("[Swish #{0}] Possible Swish: {1}; {2}; {3}; {4}", moduleId, possibleSolution[0], possibleSolution[1], possibleSolution[2], possibleSolution[3]);
        DrawCards();
    }

    void DrawCards()
    {
        for (int z = 0; z < 4; z++)
        {
            for (int s = 0; s < 16; s++)
            {
                SpriteSlots[z * 16 + s].sprite = Sprites[2];
            }
            SpriteSlots[z * 16 + cards[z][0]].sprite = Sprites[0];
            SpriteSlots[z * 16 + cards[z][1]].sprite = Sprites[1];
        }
    }

    void ControlPress(KMSelectable Control)
    {
        for (int s = 0; s < 16; s++)
        {
            if (Controls[s] == Control)
            {
                if (timerRanOut || moduleSolved) { return; }
                animq.Add(s);
                if (animating) { return; }
                ReorientCard(s);
                if (!timerStarted)
                {
                    timerStarted = true;
                    StartCoroutine(Timer());
                }
            }
        }
    }

    void ReorientCard(int s)
    {
        int c = s / 4;
        int a = s % 4;
        cards[c][0] = orientations[a][cards[c][0]];
        cards[c][1] = orientations[a][cards[c][1]];
        givenSolution[c] += ((givenSolution[c].Length != 0 ? ", " : "") + controlNames[a]);
        StartCoroutine(AnimateReorientation(c, a, false));
    }

    IEnumerator AnimateReorientation(int c, int a, bool f)
    {
        animating = true;
        float elapsed = 0f;
        float duration = 0.1f;
        if (a < 2)
        { //rotate
            var startRotation = Quaternion.Euler(0f, 0f, 0f);
            var endRotation = Quaternion.Euler(0f, (a == 0 ? -90f : 90f), 0f);
            while (elapsed < duration)
            {
                CardObjs[c].transform.localRotation = Quaternion.Slerp(startRotation, endRotation, elapsed / duration);
                yield return null;
                elapsed += Time.deltaTime;
            }
            CardObjs[c].transform.localRotation = startRotation;
        }
        else
        { //flip
            for (int r = 0; r < 2; r++)
            {
                while (elapsed < ((r == 0) ? duration / 2 : duration))
                {
                    CardObjs[c].transform.localScale = new Vector3((a == 3 ? (-elapsed * 20f) + 1f : 1f), 1f, (a == 2 ? (-elapsed * 20f) + 1f : 1f));
                    yield return null;
                    elapsed += Time.deltaTime;
                }
                if (r == 0)
                {
                    if (f && c == 0) { GeneratePuzzle(); }
                }
                else
                {
                    CardObjs[c].transform.localScale = new Vector3(1f, 1f, 1f);
                }
            }
        }
        DrawCards();
        if (!timerRanOut) { animating = false; }
        if (animq.Count() != 0) { animq.RemoveAt(0); }
        if (animq.Count() > 0)
        {
            ReorientCard(animq[0]);
        }
        else if (timerRanOut && animq.Count() == 0)
        {
            StartCoroutine(ComeTogether(1));
        }
    }

    IEnumerator Timer()
    {
        float elapsed = 0f;
        float duration = 5f;
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        timerRanOut = true;
        ControlObj.SetActive(false);
        if (animq.Count() == 0)
        {
            StartCoroutine(ComeTogether(1));
        }
    }

    IEnumerator ComeTogether(int f)
    {
        float elapsed = 0f;
        float duration = 0.5f;
        float multiplier = 0.07f * f;
        bool coinFlip = Rnd.Range(0, 2) == 0;
        while (elapsed < duration)
        {
            CardObjs[0].transform.localPosition = new Vector3((f == 1 ? -0.045f : -0.01f) + (elapsed * multiplier), 0f, (f == 1 ? 0.025f : -0.01f) - (elapsed * multiplier));
            CardObjs[1].transform.localPosition = new Vector3((f == 1 ? 0.025f : -0.01f) - (elapsed * multiplier), 0f, (f == 1 ? 0.025f : -0.01f) - (elapsed * multiplier));
            CardObjs[2].transform.localPosition = new Vector3((f == 1 ? -0.045f : -0.01f) + (elapsed * multiplier), 0f, (f == 1 ? -0.045f : -0.01f) + (elapsed * multiplier));
            CardObjs[3].transform.localPosition = new Vector3((f == 1 ? 0.025f : -0.01f) - (elapsed * multiplier), 0f, (f == 1 ? -0.045f : -0.01f) + (elapsed * multiplier));
            yield return null;
            elapsed += Time.deltaTime;
        }
        if (f == 1)
        {
            for (int z = 0; z < 4; z++)
            {
                CardObjs[z].transform.localPosition = new Vector3(-0.01f, 0f, -0.01f);
            }
            CheckAnswer();
        }
        else
        {
            CardObjs[0].transform.localPosition = new Vector3(-0.045f, 0f, 0.025f);
            CardObjs[1].transform.localPosition = new Vector3(0.025f, 0f, 0.025f);
            CardObjs[2].transform.localPosition = new Vector3(-0.045f, 0f, -0.045f);
            CardObjs[3].transform.localPosition = new Vector3(0.025f, 0f, -0.045f);
            animating = false;
            timerRanOut = false;
            timerStarted = false;
            for (int z = 0; z < 4; z++)
            {
                StartCoroutine(AnimateReorientation(z, (coinFlip ? 2 : 3), true));
            }
            duration += 0.1f;
            while (elapsed < duration)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
            ControlObj.SetActive(true);
        }
    }

    void CheckAnswer()
    {
        Debug.LogFormat("[Swish #{0}] Given Swish: {1}; {2}; {3}; {4}", moduleId, RepNone(givenSolution[0]), RepNone(givenSolution[1]), RepNone(givenSolution[2]), RepNone(givenSolution[3]));
        List<int> balls = new List<int> { cards[0][0], cards[1][0], cards[2][0], cards[3][0] };
        List<int> hoops = new List<int> { cards[0][1], cards[1][1], cards[2][1], cards[3][1] };
        balls.Sort();
        hoops.Sort();
        if (hoops.SequenceEqual(balls) && balls.Distinct().Count() == 4)
        {
            Debug.LogFormat("[Swish #{0}] That is a valid Swish, module solved.", moduleId);
            GetComponent<KMBombModule>().HandlePass();
            moduleSolved = true;
            Audio.PlaySoundAtTransform("solve", transform);
        }
        else
        {
            Debug.LogFormat("[Swish #{0}] That is not a valid Swish, strike!", moduleId);
            GetComponent<KMBombModule>().HandleStrike();
            for (int z = 0; z < 4; z++) { givenSolution[z] = ""; }
            StartCoroutine(WaitASec());
        }
    }

    string RepNone(string s)
    {
        return s == "" ? "None" : s;
    }

    IEnumerator WaitASec()
    {
        float elapsed = 0f;
        float duration = 1f;
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        StartCoroutine(ComeTogether(-1));
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} TL VHC; TR W [Do the actions on that card.] | Cards are TL, TR, BL, BR. | Actions are W (counter-clockwise), C (clockwise), V (vertical flip), H (horizontal flip. | Separate cards with semicolons.";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToLowerInvariant();
        var inputs = command.Split(';');
        var list = new List<KMSelectable>();
        for (int i = 0; i < inputs.Length; i++)
        {
            var m = Regex.Match(inputs[i], @"^\s*(?<card>TL|TR|BL|BR)\s+(?<action>(W\s*|C\s*|V\s*|H\s*)+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!m.Success)
                yield break;
            var cards = new string[] { "tl", "tr", "bl", "br" };
            int cardIx = Array.IndexOf(cards, m.Groups["card"].Value);
            foreach (var move in m.Groups["action"].Value)
            {
                int moveIx = "wcvh ".IndexOf(move);
                if (moveIx == 4)
                    continue;
                list.Add(Controls[cardIx * 4 + moveIx]);
            }
        }
        yield return null;
        for (int i = 0; i < list.Count; i++)
        {
            list[i].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }
}

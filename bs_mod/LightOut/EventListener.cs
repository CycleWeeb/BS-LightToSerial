﻿using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;

namespace LightOut
{
    class EventListener : MonoBehaviour
    {
        private SerialPort port;
        private BeatmapObjectCallbackController Ec;
        private ColorManager Cm;
        private BeatmapLevelSO BMD;
        private int BPM;
        private Color C1;
        private Color C2;
        void Awake()
        {
            Debug.Log("EventListener Created");
            port = new SerialPort("COM" + Config.comPort, Config.baudRate, Parity.None, 8, StopBits.One);
            Debug.Log("created serial connection: COM" + Config.comPort + "@" + Config.baudRate);
            port.Open();
            StartCoroutine(GrabLight());
        }

        public static Color ColourFromInt(int rgb)
        {
            rgb = rgb - 2000000000;
            int red = (rgb >> 16) & 0x0ff;
            int green = (rgb >> 8) & 0x0ff;
            int blue = (rgb) & 0x0ff;
            return new Color(red / 255f, green / 255f, blue / 255f, 1);
        }

        IEnumerator GrabLight()
        {
            yield return new WaitUntil(() => Resources.FindObjectsOfTypeAll<BeatmapObjectCallbackController>().Any());
            Ec = Resources.FindObjectsOfTypeAll<BeatmapObjectCallbackController>().FirstOrDefault();
            Debug.Log("Found LightController");
            StartCoroutine(GrabColors());
        }
        IEnumerator GrabColors()
        {
            yield return new WaitUntil(() => Resources.FindObjectsOfTypeAll<ColorManager>().Any());
            Cm = Resources.FindObjectsOfTypeAll<ColorManager>().FirstOrDefault();
            Debug.Log("Found Colors");
            StartCoroutine(GetBPM());
        }
        IEnumerator GetBPM()
        {
            yield return new WaitUntil(() => Resources.FindObjectsOfTypeAll<BeatmapLevelSO>().Any());
            BMD = Resources.FindObjectsOfTypeAll<BeatmapLevelSO>().FirstOrDefault();
            Debug.Log("Found BPM");
            Init();
        }

        void Init()
        {
            Debug.Log("Initializing..");
            Ec.beatmapEventDidTriggerEvent += EventHappened;

            C1 = Cm.ColorForNoteType(NoteType.NoteA);
            C2 = Cm.ColorForNoteType(NoteType.NoteB);

            BPM = (int)BMD.beatsPerMinute;

            port.Write(new byte[]{(byte)0, (byte)(C1.r * 255), (byte)(C1.g * 255), (byte)(C1.b * 255)},0,4);
            port.Write(new byte[]{(byte)1, (byte)(C2.r * 255), (byte)(C2.g * 255), (byte)(C2.b * 255)},0,4);

            port.Write(new byte[]{(byte)2, (byte)BPM, (byte)0, (byte)0},0,4);

            Debug.Log("C1/" + (int)(C1.r * 255) + "/" + (int)(C1.g * 255) + "/" + (int)(C1.b * 255));
            Debug.Log("C2/" + (int)(C2.r * 255) + "/" + (int)(C2.g * 255) + "/" + (int)(C2.b * 255));
        }

        void OnDestroy()
        {
            Debug.Log("Removing Eventlistener");
            port.Close();
        }

        void EventHappened(BeatmapEventData Data)
        {
            int Event;
            int value = Data.value;
            Int32.TryParse(Data.type.ToString().Replace("Event", ""), out Event);
            if (value < 2000000000)
            {
                port.Write(new byte[] { (byte)3, byte.Parse(Data.type.ToString().Replace("Event", "")), (byte)value, (byte)0 }, 0, 4);
            }else if (Config.chroma)
            {
                Color C = ColourFromInt(value);
                //Debug.Log((4 + Event) + "    " + (int)(C.r * 255) + "    " + (int)(C.g * 255) + "    " + (int)(C.b * 255));
                port.Write(new byte[] { (byte)(4 + Event), (byte)(C.r * 255), (byte)(C.g * 255), (byte)(C.b * 255) }, 0, 4);

            }
            //Debug.Log(Data.type.ToString().Replace("Event", "") + "/" + Data.value);
        }
    }
}

﻿using System; 
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Nikse.SubtitleEdit.Logic.SubtitleFormats
{
    class CheetahCaption : SubtitleFormat
    {

        public override string Extension
        {
            get { return ".cap"; }
        }

        public override string Name
        {
            get { return "Cheetah Caption"; }
        }

        public override bool HasLineNumber
        {
            get { return false; }
        }

        public override bool IsTimeBased
        {
            get { return true; }
        }

        public void Save(string fileName, Subtitle subtitle)
        {
            var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);

            byte[] buffer = new byte[] { 0xEA, 0x22, 1, 0 }; // header
            fs.Write(buffer, 0, buffer.Length);

            buffer = new byte[] { 5, 0, 9, 0xA8, 0xAF, 0x4F }; // sizes
            fs.Write(buffer, 0, buffer.Length);

            for (int i = 0; i < 118; i++)
                fs.WriteByte(0);

            // paragraphs
            foreach (Paragraph p in subtitle.Paragraphs)
            {
                string text = p.Text.Replace(Environment.NewLine, "\0\0\0\0");
                int length = text.Length + 16;
                long end = fs.Position + length;
                fs.WriteByte((byte)(length));

                fs.WriteByte(0x42); // ?

                WriteTime(fs, p.StartTime); 

                buffer = new byte[] { 0x12, 1,0,0,0,0,1,0x0F,1 }; // sizes ?
                fs.Write(buffer, 0, buffer.Length);

                buffer = Encoding.ASCII.GetBytes(text);
                fs.Write(buffer, 0, buffer.Length); // Text starter på index 19 (0 baseret)

                while (end > fs.Position)
                    fs.WriteByte(0);
            }
            fs.Close();
        }

        private void WriteTime(FileStream fs, TimeCode timeCode)
        {
            fs.WriteByte((byte)timeCode.Hours);
            fs.WriteByte((byte)timeCode.Minutes);
            fs.WriteByte((byte)timeCode.Seconds);
            fs.WriteByte((byte)MillisecondsToFrames(timeCode.Milliseconds));
        }

        public override bool IsMine(List<string> lines, string fileName)
        {
            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                var fi = new FileInfo(fileName);
                if (fi.Length >= 200 && fi.Length < 1024000) // not too small or too big
                {
                    if (fileName.ToLower().EndsWith(".cap"))
                    {
                        byte[] buffer = File.ReadAllBytes(fileName);
                        for (int i = 0; i < buffer.Length - 20; i++)
                        {
                            if (buffer[i + 0] == 0xEA &&
                                buffer[i + 1] == 0x22 &&
                                buffer[i + 2] == 0x01)
                                return true;
                        }
                    }
                }
            }
            return false;
        }

        public override string ToText(Subtitle subtitle, string title)
        {
            return "Not supported!";
        }

        private TimeCode DecodeTimeStamp(byte[] buffer, int index)
        {
            return new TimeCode(buffer[index], buffer[index+1], buffer[index+2], FramesToMilliseconds(buffer[index+3]));
        }

        public override void LoadSubtitle(Subtitle subtitle, List<string> lines, string fileName)
        {
            subtitle.Paragraphs.Clear();
            subtitle.Header = null;
            byte[] buffer = File.ReadAllBytes(fileName);

            int i = 128;
            Paragraph last = null;
            while (i < buffer.Length - 20)
            {
                var p = new Paragraph();
                int length = buffer[i];
                int textLength = length - 20;
                int start = 19;
                for (int j=0; j<4; j++)
                {
                    if (buffer[i + start - 1] > 0x10)
                    {
                        start--;
                        textLength++;
                    }
                }
                if (textLength > 0 && buffer.Length >= i+textLength)
                {
                    byte firstByte = buffer[i + 1];
                    p.StartTime = DecodeTimeStamp(buffer, i + 2);

                    if (last != null && last.EndTime.TotalMilliseconds > p.StartTime.TotalMilliseconds)
                        last.EndTime.TotalMilliseconds = p.StartTime.TotalMilliseconds - Configuration.Settings.General.MininumMillisecondsBetweenLines;

                    p.EndTime = DecodeTimeStamp(buffer, i + 6);
                    p.Text = Encoding.ASCII.GetString(buffer, i + start, textLength);
                    p.Text = p.Text.Replace("\0\0\0\0", Environment.NewLine);
                    subtitle.Paragraphs.Add(p);
                    last = p;
                }
                if (length == 0)
                    length++;
                i += length;
            }
            if (last != null && last.Duration.TotalMilliseconds > Configuration.Settings.General.SubtitleMaximumDisplayMilliseconds)
                last.EndTime.TotalMilliseconds = last.StartTime.TotalMilliseconds + Utilities.GetDisplayMillisecondsFromText(last.Text);

            subtitle.Renumber(1);
        }

    }
}
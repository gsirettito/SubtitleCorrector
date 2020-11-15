using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SiretT.Controls {
    public class Subtitle {
        List<SubRipFormat> textTimes;
        public Subtitle() {
            textTimes = new List<SubRipFormat>();
        }

        public TimeSpan Offset { get; set; }

        public SubRipFormat this[int index] { get { return textTimes[index]; } }

        public List<SubRipFormat> Texts { get { return textTimes; } }

        public static Subtitle FromFile(string srtfile) {
            Subtitle subSrt = new Subtitle();
            using (StreamReader sr = new StreamReader(srtfile, getEncoding(srtfile))) {
                while (!sr.EndOfStream) {
                    SubRipFormat srt = new SubRipFormat();
                    string line = sr.ReadLine();
                    if (string.IsNullOrEmpty(line)) continue;
                    srt.Index = int.Parse(line);
                    var times = sr.ReadLine().Replace(" --> ", "/").Split('/');
                    srt.StartTime = TimeSpan.Parse(times[0]);
                    srt.EndTime = TimeSpan.Parse(times[1]);
                    string text = "";
                    while (true) {
                        line = sr.ReadLine();
                        if (string.IsNullOrEmpty(line)) break;
                        text += line + Environment.NewLine;
                    }
                    srt.Text = text.Substring(0, text.Length - 2);
                    subSrt.textTimes.Add(srt);
                }
            }
            return subSrt;
        }

        private static Encoding getEncoding(string path) {
            var stream = new FileStream(path, FileMode.Open);
            var reader = new StreamReader(stream, Encoding.Default, true);
            reader.Read();
            var enc = reader.CurrentEncoding;
            if (reader.CurrentEncoding != Encoding.Default) {
                reader.Close();
                return enc;
            }

            stream.Position = 0;

            reader = new StreamReader(stream, new UTF8Encoding(false, true));
            try {
                reader.ReadToEnd();
                reader.Close();
                return Encoding.UTF8;
            } catch (Exception) {
                reader.Close();
                return Encoding.Default;
            }
        }

        public void Move(int index, TimeSpan beginTime) {
            if (index < 0 || index > textTimes.Count)
                throw new IndexOutOfRangeException();
            TimeSpan offset = TimeSpan.FromSeconds(0);
            offset = textTimes[index].StartTime - beginTime;
            var duration = textTimes[index].EndTime - textTimes[index].StartTime;
            textTimes[index].StartTime = beginTime;
            textTimes[index].EndTime = beginTime + duration;
            index++;
            while (index < textTimes.Count) {
                duration = textTimes[index].EndTime - textTimes[index].StartTime;
                textTimes[index].StartTime -= offset;
                textTimes[index].EndTime = textTimes[index].StartTime + duration;
                index++;
            }
        }

        public void Move(TimeSpan offset) {
            Move(offset, 0);
        }

        public void Move(TimeSpan offset, int fromIndex) {
            int index = fromIndex;
            if (index < 0 || index > textTimes.Count)
                throw new IndexOutOfRangeException();
            while (index < textTimes.Count) {
                var duration = textTimes[index].EndTime - textTimes[index].StartTime;
                textTimes[index].StartTime -= offset;
                textTimes[index].EndTime = textTimes[index].StartTime + duration;
                index++;
            }
        }

        public void RemoveAt(int index) {
            if (index < 0 || index > textTimes.Count)
                throw new IndexOutOfRangeException();
            textTimes.RemoveAt(index);
            while (index < textTimes.Count) {
                textTimes[index++].Index -= 1;
            }
        }

        public void Expand(int index, TimeSpan time) {
            if (index < 0 || index > textTimes.Count)
                throw new IndexOutOfRangeException();
            textTimes[index].EndTime += time;
            index++;
            Move(-time, index);
        }

        public void Shrink(int index, TimeSpan time) {
            if (index < 0 || index > textTimes.Count)
                throw new IndexOutOfRangeException();
            textTimes[index].EndTime -= time;
            index++;
            Move(time, index);
        }

        public void Save(string srtfile) {
            using (StreamWriter sw = new StreamWriter(srtfile, false, UnicodeEncoding.UTF8)) {
                foreach (var i in textTimes) {
                    sw.WriteLine(i.Index);
                    sw.WriteLine(string.Format("{0} --> {1}",
                        i.StartTime.ToString("hh':'mm':'ss\\,fff"),
                        i.EndTime.ToString("hh':'mm':'ss\\,fff")));
                    sw.WriteLine(i.Text);
                    sw.WriteLine();
                }
                sw.Close();
            }
        }
    }
}

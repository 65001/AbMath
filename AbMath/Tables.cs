using System;
using System.Collections.Generic;
using System.Text;

namespace CLI
{
    public enum Format { Default, MarkDown };
    public struct Config
    {
        public string Title { get; set; }
        public Format Format;
    }

    public struct Schema
    {
        public string Column { get; set; }
        public int Width { get; set; }
    }

    struct Cursor
    {
        public int beginy;
        public int endy;
        public bool Exists;
    }

    public static class CharacterSheetFactory
    {
        public static CharacterSheet Default()
        {
            return new CharacterSheet
            {
                TopLeft = '┌',
                TopRight = '┐',

                Continue = '─',
                Down = '│',

                MidLeft = '├',
                MidRight = '┤',
                MidTerminate = '┬',

                BottomLeft = '└',
                BottomTerminate = '┴',
                BottomRight = '┘'
            }; 
        }

        public static CharacterSheet MarkDown() {
            return new CharacterSheet {
                TopLeft = '-',
                TopRight = '-',

                Continue = '-',
                Down = '|',

                MidLeft = '|',
                MidRight = '|',
                MidTerminate = '-',

                BottomLeft = '-',
                BottomTerminate = '-',
                BottomRight = '-'
            };
        }
    }

    /// <summary>
    /// Replace these characters with a Unicode set if
    /// the characters don't display in your locale
    /// </summary>
    public struct CharacterSheet
    {
        public char TopLeft;
        public char TopRight;

        public char Down;
        public char Continue;

        public char MidLeft;
        public char MidTerminate;
        public char MidRight;

        public char BottomLeft;
        public char BottomTerminate;
        public char BottomRight;
    }

    public class Tables<T>
    {
        private List<Schema> schemas;
        private List<T[]> data;
        private Config config;
        private Cursor cursor;
        public bool SuggestedRedraw { get; private set; }
        public CharacterSheet Sheet;

        public Tables(Config Config)
        {
            config = Config;
            schemas = new List<Schema>();
            data = new List<T[]>();
            try
            {
                cursor = new Cursor { beginy = Console.CursorTop, Exists = true };
            }
            catch(Exception ex)
            {
                cursor = new Cursor {Exists = false };
            }
            //AutoGeneration
            switch (config.Format)
            {
                case Format.MarkDown:
                    Sheet = CharacterSheetFactory.MarkDown();
                    break;
                case Format.Default:
                default:
                    Sheet = CharacterSheetFactory.Default();
                    break;
            }
        }

        public Tables<T> Add(Schema schema)
        {
            schemas.Add(schema);
            return this;
        }

        public Tables<T> Add(T[] row)
        {
            for (int i = 0; i < row.Length; i++)
            {
                if (row[i] == null)
                {
                    throw new ArgumentNullException();
                }
            }
            data.Add(row);
            return this;
        }

        //Top
        //Title
        //Headers
        public string GenerateHeaders()
        {
            var sb = new StringBuilder();
            StringBuilder md = (config.Format == Format.MarkDown) ? new StringBuilder() : null;

            int sum = TableWidth();
            int floor = (int)Math.Floor((decimal)sum / 2);
            int ceiling = (int)Math.Ceiling((decimal)sum / 2);
            int Length = config.Title.Length;


            if (config.Format == Format.Default)
            {
                sb.AppendLine($"{Sheet.TopLeft}{"".PadRight(sum, Sheet.Continue)}{Sheet.TopRight}");
                sb.AppendLine(
                    $"{Sheet.Down}{"".PadRight(floor - Length)}{config.Title}{"".PadRight(ceiling)}{Sheet.Down}");
                sb.AppendLine(Lines(new char[] { Sheet.MidLeft, Sheet.MidTerminate, Sheet.MidRight }));
            }
            else
            {
                sb.AppendLine($"# {config.Title}");
            }

            sb.Append(Sheet.Down);
            md?.Append("\n");
            for (int i = 0; i < schemas.Count; i++)
            {
                int dif = schemas[i].Width - schemas[i].Column.Length;
                sb.Append(Row(schemas[i].Column, dif, i));
                md?.Append("|-");
            }

            md?.Append("|");

            return sb.ToString() + md?.ToString();
        }

        //Data
        public string GenerateBody()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < data.Count; i++)
            {
                sb.AppendLine( GenerateRow(i));
            }
            return sb.ToString();
        }

        public string GenerateNextRow()
        {
            return GenerateRow(data.Count - 1);
        }

        private string GenerateRow(int index)
        {
            if (index > data.Count)
            {
                throw new IndexOutOfRangeException($"The Index was {index} but the max is {data.Count}!");
            }

            if (schemas.Count != data[index].Length)
            {
                throw new ArgumentOutOfRangeException($"Was given {data[index].Length} args but expected {schemas.Count}");
            }


            var sb = new StringBuilder();
            sb.Append(Sheet.Down);
            for (int i = 0; i < schemas.Count; i++)
            {
                sb.Append(Row(data[index][i].ToString() ?? string.Empty, schemas[i].Width - data[index][i].ToString().Length, i));
            }
            return sb.ToString();
        }

        //End Data segment
        //any addtional footers?
        public string GenerateFooter()
        {
            string footer = Lines(new char[] { Sheet.BottomLeft, Sheet.BottomTerminate, Sheet.BottomRight });
            if (cursor.Exists)
            {
                cursor.endy = Console.CursorTop + 1;
            }
            return footer;
        }

        private string Lines(char[] chars) {
            var sb = new StringBuilder();
            sb.Clear();
            sb.Append(chars[0]);

            string data;
            for (int i = 0; i < schemas.Count; i++)
            {
                data = string.Empty;
                if (i == (schemas.Count - 1))
                {
                    data = $"{"".PadRight(schemas[i].Width + 2, Sheet.Continue)}";
                }
                else
                {
                    data = $"{"".PadRight(schemas[i].Width + ((i == 0) ? 1 : 2), Sheet.Continue)}{chars[1]}";
                }
                sb.Append(data);
            }
            sb.Append($"{chars[2]}");

            return sb.ToString();
        }

        private string Row(string output, int dif, int i) {
            var temp = string.Empty;
            if (dif <= 0)
            {
                //Overrides user width suggestion when an overflow occurs.
                schemas[i] = new Schema { Column = schemas[i].Column, Width = schemas[i].Width + Math.Abs(dif) };
                SuggestedRedraw = true;
                temp = (i == 0) ? $"{output}{Sheet.Down}" : $" {output} {Sheet.Down}";
            }
            else
            {
                temp = (i == 0) ? $"{output}{"".PadRight(dif)} {Sheet.Down}" : $" {output}{"".PadRight(dif)} {Sheet.Down}";
            }
            return temp;
        }

        private int TableWidth()
        {
            int sum = schemas.Count - 1;
            for (int i = 0; i < schemas.Count; i++)
            {
                sum += (i == 0) ? 1 : 2;
                sum += schemas[i].Width;
            }
            return sum;
        }

        public string Redraw()
        {
            Clear();
            return ToString();
        }

        public void Clear()
        {
            if (cursor.Exists)
            {
                for (int i = cursor.beginy; i < cursor.endy; i++)
                {
                    Clear(i);
                }
                Console.SetCursorPosition(0, cursor.beginy);
            }
        }

        void Clear(int y)
        {
            Console.SetCursorPosition(0, y);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, y);
        }

        public override string ToString()
        {
            return GenerateHeaders() +"\n"+ GenerateBody() + GenerateFooter();
        }
    }
}

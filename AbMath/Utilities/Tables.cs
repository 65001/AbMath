using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Utilities
{
    public enum Format { Default, MarkDown };
    public struct Config
    {
        public string Title { get; set; }
        public Format Format;
    }

    public struct Schema
    {
        public Schema(string column)
        {
            this.Column = column;
            this.Width = column.Length + Tables.Padding;
        }

        public Schema(string column, int width)
        {
            this.Column = column;
            this.Width = width;
        }

        public string Column { get; set; }
        public int Width { get; set; }
    }

    internal struct Cursor
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

    public class Tables
    {
        public const int LeftPadding = 1;
        public const int RightPadding = 1;
        public const int Padding = LeftPadding + RightPadding;
    }

    public class Tables<T> : Tables
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
            if (data.Count > 0)
            {
                //If we have data already we cannot add to the schema
                throw new Exception("You cannot add to the schema after you have added a row to your table.");
            }
            //If the schema width is bad we should adjust it without telling the user! 
            schema.Width = Math.Max(schema.Column.Length + RightPadding, schema.Width);
            schemas.Add(schema);
            return this;
        }

        public Tables<T> Add(T[] row)
        {
            for (int i = 0; i < row.Length; i++)
            {
                if (row[i] == null)
                {
                    throw new ArgumentNullException($"null at {i}. Table {config.Title}");
                }

                if (row[i].ToString().Length > schemas[i].Width)
                {
                    schemas[i] = new Schema(schemas[i].Column, row[i].ToString().Length + RightPadding); 
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
            int floor = (int)Math.Floor((decimal)(sum + config.Title.Length)/ 2);
            int ceiling = (int)Math.Ceiling((decimal)(sum - config.Title.Length) / 2);
            int Length = config.Title.Length;


            if (config.Format == Format.Default)
            {
                sb.Append(Sheet.TopLeft);
                sb.Append(Sheet.Continue, sum);
                sb.Append(Sheet.TopRight);
                sb.AppendLine();

                sb.Append(Sheet.Down);
                sb.Append(' ', floor - Length);
                sb.Append(config.Title);
                sb.Append(' ', ceiling);
                sb.Append(Sheet.Down);
                sb.AppendLine();

                Lines(new char[] {Sheet.MidLeft, Sheet.MidTerminate, Sheet.MidRight}, sb);
                sb.AppendLine();
            }
            else
            {
                sb.Append("# ");
                sb.Append(config.Title);
                sb.AppendLine();
            }

            sb.Append(Sheet.Down);
            md?.Append("\n");
            for (int i = 0; i < schemas.Count; i++)
            {
                int dif = schemas[i].Width - schemas[i].Column.Length;
                Row(schemas[i].Column, dif, i, sb);
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
                if (schemas.Count != data[i].Length)
                {
                    throw new ArgumentOutOfRangeException($"Was given {data[i].Length} args but expected {schemas.Count}");
                }

                sb.Append(Sheet.Down);
                for (int j = 0; j < schemas.Count; j++)
                {
                    Row(data[i][j].ToString() ?? string.Empty, schemas[j].Width - data[i][j].ToString().Length, j, sb);
                }

                sb.Append("\n");
            }
            return sb.ToString();
        }

        //End Data segment
        //any addtional footers?
        public string GenerateFooter()
        {
            StringBuilder sb = new StringBuilder();
            Lines(new char[] { Sheet.BottomLeft, Sheet.BottomTerminate, Sheet.BottomRight }, sb);
            if (cursor.Exists)
            {
                cursor.endy = Console.CursorTop + 1;
            }
            return sb.ToString();
        }

        private void Lines(char[] chars, StringBuilder sb) {
            sb.Append(chars[0]);

            for (int i = 0; i < schemas.Count; i++)
            {
                if (i == (schemas.Count - 1))
                {
                    sb.Append( new string(Sheet.Continue, schemas[i].Width + 2));
                }
                else
                {
                    sb.Append( new string(Sheet.Continue, schemas[i].Width + ((i == 0) ? 1 : 2)) + chars[1]);
                }
            }
            sb.Append($"{chars[2]}");
        }

        private void Row(string output, int dif, int i, StringBuilder sb) {
            //If the diff is negative it means that the column is not big enough! 
            if (dif < 0)
            {
                //Overrides user width suggestion when an overflow occurs.
                throw new Exception($"Table overflow occured!\n{config.Title}\n{output}\n{i}\n{dif}\n{schemas[i].Width}");
            }

            if (i != 0)
            {
                sb.Append(" ");
            }

            sb.Append(output);
            sb.Append(' ', dif + 1);
            sb.Append(Sheet.Down);
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
            StringBuilder sb = new StringBuilder();
            sb.Append(GenerateHeaders());
            sb.AppendLine();
            sb.Append(GenerateBody());
            sb.Append(GenerateFooter());
            return sb.ToString();
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Peg
{
    static class PegUtil
    {
        public static String EscapeSpecials(this String self)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < self.Length; ++i)
            {
                switch (self[i])
                {
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    default:
                        sb.Append(self[i]);
                        break;
                }
            }
            return sb.ToString();
        }

        public static void GetRowCol(this string self, int index, out int row, out int col)
        {
            row = 0;
            int nLastRow = 0;
            for (int i = 0; i < index; ++i)
            {
                if (self[i].Equals('\n'))
                {
                    row++;
                    nLastRow = i;
                }
            }
            col = index - nLastRow;
        }

        public static string SafeSubstring(this string self, int begin, int count)
        {
            if (begin < 0)
            {
                count += begin;
                begin = 0;
            }
            if (begin >= self.Length)
            {
                begin = self.Length - 1;
                count = 0;
            }
            if (begin + count > self.Length)
            {
                count = self.Length - begin;
            }
            if (count < 0)
            {
                count = 0;
            }
            return self.Substring(begin, count);
        }

        public static int IndexOfNthChar(this string s, char c, int n)
        {
            int cnt = 0;
            for (int i = 0; i < s.Length; ++i)
                if (s[i] == c)
                    if (++cnt == n)
                        return i;
            return -1;
        }

        public static int CountChar(this string s, char c)
        {
            int r = 0;
            for (int i = 0; i < s.Length; ++i)
                if (s[i] == c)
                    ++r;
            return r;
        }

        public static int LineOfIndex(this string s, int index)
        {
            return s.Substring(0, index).CountChar('\n');
        }

        public static int GetIndexOfCharBefore(this string s, char c, int n)
        {
            while (--n >= 0)
            {
                if (s[n] == c)
                    return n;
            }
            return -1;
        }

        public static int GetIndexOfCharAfter(this string s, char c, int n)
        {
            int len = s.Length;
            while (++n < len)
            {
                if (s[n] == c)
                    return n;
            }
            return -1;
        }

        public static string GetLine(this string s, int index)
        {
            int begin = index - 1;
            while (begin >= 0)
            {
                if (begin == 0 || s[begin - 1] == '\n')
                    break;
                --begin;
            }
            int end = index;
            while (end < s.Length)
            {
                if (s[end] == '\n')
                    break;
                ++end;
            }
            int cnt = end - begin;
            return s.SafeSubstring(begin, cnt);
        }
    }
}

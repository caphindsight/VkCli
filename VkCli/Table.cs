using System;
using System.Collections.Generic;
using System.Linq;

namespace VkCli {
    public sealed class Table {
        private List<string[]> Data_ = new List<string[]>();

        public void Add(params object[] fields) {
            string[] copy = (
                from i in fields
                select i != null ? i.ToString() : ""
            ).ToArray();
            Data_.Add(copy);
        }

        public void SortBy(int k) {
            Data_.Sort((a, b) => {
                if (a.Length <= k) {
                    return b.Length <= k ? 0 : 1;
                }

                return String.Compare(a[k], b[k]);
            });
        }

        public void Display() {
            if (Data_.Count == 0)
                return;

            int n = (
                from r in Data_
                select r.Length
            ).Max();

            if (n == 0)
                return;

            int[] sz = new int[n];
            for (int i = 0; i < n; i++) {
                sz[i] = (
                    from r in Data_
                    where i < r.Length
                    select r[i].Length
                ).Max();
            }

            foreach (string[] r in Data_) {
                for (int i = 0; i < r.Length; i++) {
                    if (i != 0)
                        Console.Write("  ");

                    Console.Write(MiscUtils.FitString(r[i], sz[i]));
                }
                Console.WriteLine();
            }
        }
    }
}

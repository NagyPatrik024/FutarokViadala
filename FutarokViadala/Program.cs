using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FutarokViadala
{
    class Program
    {
        static void Main(string[] args)
        {
            List<Rendeles> rendelesek = Enumerable.Range(1, 10).Select(x => new Rendeles()).ToList();
            List<Futar> futarok = Enumerable.Range(1, 4).Select(x => new Futar(FutarCeg.FurgeFutar)).ToList();
            futarok.AddRange(Enumerable.Range(1, 4).Select(x => new Futar(FutarCeg.TurboTeknos)));

            List<Task> ts = rendelesek.Select(x => new Task(() => { x.Work(); }, TaskCreationOptions.LongRunning)).ToList();
            ts.AddRange(futarok.Select(x => new Task(() => { x.Work(rendelesek); }, TaskCreationOptions.LongRunning)));

            ts.Add(new Task(() =>
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                TimeSpan timespan;
                while (rendelesek.Any(x => x.Status != RendelesStatus.Kiszallitva))
                {
                    Console.Clear();
                    foreach (var f in futarok)
                    {
                        Console.WriteLine(f);
                    }
                    Console.WriteLine();
                    foreach (var r in rendelesek.Where(x => x.Status != RendelesStatus.Init && x.Status != RendelesStatus.Kiszallitva))
                    {
                        Console.WriteLine(r);
                    }
                    Console.WriteLine();
                    timespan = stopwatch.Elapsed;
                    Console.WriteLine(string.Format("{0} óra {1} perc", Math.Floor(timespan.TotalMinutes), timespan.ToString("ss")));
                    Thread.Sleep(50);
                }
                stopwatch.Stop();
                Console.Clear();
                Console.WriteLine("Szimuláció véget ért");
                timespan = stopwatch.Elapsed;
                Console.WriteLine(string.Format("{0} óra {1} perc", Math.Floor(timespan.TotalMinutes), timespan.ToString("ss")));
            }, TaskCreationOptions.LongRunning));

            ts.ForEach(x => x.Start());
            Console.ReadKey();
        }
    }
    static class Util
    {
        public static Random rnd = new Random();
    }
    enum RendelesStatus { Init, Összeallitva, FutarraVar, Futarnal, Kiszallitva }
    class Rendeles
    {
        public static List<Rendeles> rendelesek = new List<Rendeles>();
        static int _id = 0;
        public int Id { get; set; }
        public RendelesStatus Status { get; set; }
        public int Erteke { get; set; }
        public int Tavolsag { get; set; }

        public static object valasztoLock = new object();

        public Rendeles()
        {
            Id = _id++;
            Status = RendelesStatus.Init;
            Erteke = 0;
            Tavolsag = 0;
        }

        public void Work()
        {
            Thread.Sleep(Id * Util.rnd.Next(1000, 5001));
            Status = RendelesStatus.Összeallitva;
            Erteke = Util.rnd.Next(2000, 10001);
            Tavolsag = Util.rnd.Next(500, 10001);
            Status = RendelesStatus.FutarraVar;
            lock (valasztoLock)
            {
                rendelesek.Add(this);
            }
        }

        public override string ToString()
        {
            return $"Rendeles {Id} : {Status}";
        }
    }

    enum FutarStatus { Init, RendelesreVar, Szallit, Atad, Visszater, Vegzett }
    enum FutarCeg { FurgeFutar, TurboTeknos }
    class Futar
    {
        static int _id = 0;
        public int Id { get; set; }
        public Rendeles Rendeles { get; set; }
        public FutarCeg Ceg { get; set; }
        public FutarStatus Status { get; set; }
        public double Fizetes { get; set; }


        public Futar(FutarCeg ceg)
        {
            Id = _id++;
            Rendeles = null;
            Ceg = ceg;
            Status = FutarStatus.Init;

        }

        public void Work(List<Rendeles> rendelesek)
        {
            while (rendelesek.Any(x => x.Status != RendelesStatus.Kiszallitva))
            {
                Status = FutarStatus.RendelesreVar;
                Rendeles r = null;
                while (r == null)
                {
                    if (Ceg == FutarCeg.FurgeFutar)
                    {
                        lock (Rendeles.valasztoLock)
                        {
                            r = Rendeles.rendelesek.Where(x=> x.Status == RendelesStatus.FutarraVar).OrderByDescending(x => x.Tavolsag).FirstOrDefault();
                        }
                        if (r == null)
                        {
                            Thread.Sleep(Util.rnd.Next(1000, 2001));
                        }
                    }
                    else if (Ceg == FutarCeg.TurboTeknos)
                    {
                        lock (Rendeles.valasztoLock)
                        {
                            r = Rendeles.rendelesek.Where(x => x.Status == RendelesStatus.FutarraVar).OrderBy(x => x.Tavolsag).FirstOrDefault();
                        }
                        if (r == null)
                        {
                            Thread.Sleep(Util.rnd.Next(1000, 2001));
                        }
                    }
                }
                Rendeles = r;
                r.Status = RendelesStatus.Futarnal;
                double sebesseg = Util.rnd.Next(20, 40) / 10;
                Status = FutarStatus.Szallit;
                Thread.Sleep((int)(r.Tavolsag / sebesseg));
                Status = FutarStatus.Atad;
                Thread.Sleep(Util.rnd.Next(2000, 5001));
                r.Status = RendelesStatus.Kiszallitva;
                Status = FutarStatus.Visszater;
                Thread.Sleep((int)(r.Tavolsag / sebesseg));
                if (Ceg == FutarCeg.FurgeFutar)
                {
                    Fizetes += 600;
                }
                else if (Ceg == FutarCeg.TurboTeknos)
                {
                    Fizetes += (r.Erteke * 0.05);
                    if (r.Tavolsag > 3000)
                    {
                        Fizetes += (int)((r.Tavolsag - 3000 / 1000) + 1) * 200;
                    }
                }
                Rendeles = null;
            }
            Status = FutarStatus.Vegzett;
        }

        public override string ToString()
        {
            if (Rendeles != null)
            {
                return $"Futar {Id} : {Status} Rendeles : {Rendeles.Id}";
            }
            return $"Futar {Id} : {Status}";
        }
    }
}

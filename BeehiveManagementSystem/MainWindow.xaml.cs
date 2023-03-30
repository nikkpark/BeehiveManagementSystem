using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BeehiveManagementSystem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer = new DispatcherTimer();
        private Queen queen = new Queen();
        public MainWindow()
        {
            
            InitializeComponent();
            statusReport.Text = queen.StatusReport;
            timer.Tick += Timer_Tick;
            timer.Interval = TimeSpan.FromSeconds(1.5);
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            WorkShift_Click(this, new RoutedEventArgs());
        }

        private void WorkShift_Click(object sender, RoutedEventArgs e)
        {
            queen.WorkTheNextShift();
            statusReport.Text = queen.StatusReport;
        }

        private void AssignJob_Click(object sender, RoutedEventArgs e)
        {
            queen.AssignBee(jobSelector.Text);
            statusReport.Text = queen.StatusReport;
        }
    }

    public static class HoneyVault
    {
        public const float NECTAR_CONVERSION_RATIO = 0.21f;
        public const float LOW_LEVEL_WARNING = 10f;
        private static float nectar = 200f;
        private static float honey = 40f;
        public static string StatusReport
        {
            get
            {
                string report = $"Nectar: {nectar:0.0}.\nHoney: {honey:0.0}.\n";
                string warnings = "";
                if (nectar < LOW_LEVEL_WARNING)
                {
                    warnings += "LOW NECTAR - ADD A NECTAR COLLECTOR.\n";
                }
                if (honey < LOW_LEVEL_WARNING)
                {
                    warnings += "LOW HONEY - ADD A HONEY MANUFACTURER\n";
                }
                return report + warnings;
            }
        }
        public static void CollectNectar(float amount)
        {
            if (amount > 0f) nectar += amount;
        }
        public static void ConvertNectarToHoney(float amount)
        {
            if (amount > nectar)
            {
                nectar -= nectar;
                honey += nectar * NECTAR_CONVERSION_RATIO;
            }
            else
            {
                nectar -= amount;
                honey += amount * NECTAR_CONVERSION_RATIO;
            }
        }
        public static bool ConsumeHoney(float amount)
        {
            if (amount < honey)
            {
                honey -= amount;
                return true;
            }
            else return false;
        }
    }
    public abstract class Bee
    {
        public abstract float CostPerShift { get; }
        public string Job { get; private set; }
        public Bee(string job)
        {
            Job = job;
        }      
        public void WorkTheNextShift()
        {
            if (HoneyVault.ConsumeHoney(CostPerShift))
            {
                DoJob();
            }
        }
        protected abstract void DoJob();
    }
    public class Queen : Bee
    {
        public const float EGGS_PER_SHIFT = 0.45f;
        public const float HONEY_PER_UNASSIGNED_WORKER = 0.5f;

        private Bee[] workers = new Bee[0];
        private float unassignedWorkers = 3;
        private float eggs = 0;

        public string StatusReport { get; private set; }
        public override float CostPerShift { get { return 2.15f; } }
        public Queen() : base("Queen")
        {
            AssignBee("Nectar Collector");
            AssignBee("Honey Manufacturer");
            AssignBee("Egg Care");
        }        
        private void AddWorker(Bee worker)
        {
            if (unassignedWorkers >= 1)
            {
                unassignedWorkers--;
                Array.Resize(ref workers, workers.Length + 1);
                workers[workers.Length - 1] = worker;
            }
        }
        private void UpdateStatusReport()
        {
            StatusReport = $"Vault report:\n{HoneyVault.StatusReport}\n" +
            $"\nEgg count: {eggs:0.0}\nUnassigned workers: {unassignedWorkers:0.0}\n" +
            $"{WorkerStatus("Nectar Collector")}\n{WorkerStatus("Honey Manufacturer")}" +
            $"\n{WorkerStatus("Egg Care")}\nTOTAL WORKERS: {workers.Length}";
        }
        public void AssignBee(string job)
        {
            switch(job)
            {
                case "Egg Care":
                    AddWorker(new EggCare(this));
                    break;
                case "Nectar Collector":
                    AddWorker(new NectarCollector());
                    break;
                case "Honey Manufacturer":
                    AddWorker(new HoneyManufacturer());
                    break;
            }
            UpdateStatusReport();
        }
        public void CareForEggs(float eggsToConvert)
        {
            if (eggs >= eggsToConvert)
            {
                eggs -= eggsToConvert;
                unassignedWorkers += eggsToConvert;
            }
            else
            {
                unassignedWorkers += 0;
            }
        }
        private string WorkerStatus(string job)
        {
            int count = 0;
            foreach (Bee worker in workers)
            {
                if (worker.Job == job) count++;
            }
            string s = "s";
            if (count == 1) s = "";
            return $"{count} {job} bee{s}";
        }
        protected override void DoJob()
        {
            eggs += EGGS_PER_SHIFT;
            foreach (Bee worker in workers)
            {
                worker.WorkTheNextShift();
            }
            HoneyVault.ConsumeHoney(HONEY_PER_UNASSIGNED_WORKER * workers.Length);
            UpdateStatusReport();
        }

    }
    public class HoneyManufacturer: Bee
    {
        public const float NECTAR_PROCESSED_PER_SHIFT = 33.15f;
        public override float CostPerShift { get; } = 1.7f;
        public HoneyManufacturer() : base("Honey Manufacturer") { }        
        protected override void DoJob()
        {
            HoneyVault.ConvertNectarToHoney(NECTAR_PROCESSED_PER_SHIFT);
        }
    }
    public class NectarCollector : Bee
    {
        public const float NECTAR_COLLECTED_PER_SHIFT = 33.25f;
        public override float CostPerShift { get; } = 1.95f;
        public NectarCollector() : base("Nectar Collector") { }        
        protected override void DoJob()
        {
            HoneyVault.CollectNectar(NECTAR_COLLECTED_PER_SHIFT);
        }
    }
    public class EggCare : Bee
    {
        public const float CARE_PROGRESS_PER_SHIFT = 0.15f;
        public override float CostPerShift { get; } = 1.35f;
        private Queen queen;
        public EggCare(Queen queen) : base("Egg Care")
        {
            this.queen = queen;
        }        
        protected override void DoJob()
        {
            queen.CareForEggs(CARE_PROGRESS_PER_SHIFT);
        }        
    }
}

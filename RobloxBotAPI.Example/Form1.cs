using RobloxBotAPI.JsonResult;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RobloxBotAPI.Example
{
    public partial class Form1 : Form
    {

        RobloxBot bot;
        LoginResult loginResult;

        public Form1()
        {
            InitializeComponent();
        }

        T RunAsyncCommand<T>(Task<T> task)
        {
            return AsyncHelpers.RunSync<T>(() => task);
        }

        void RunAsyncCommand(Task task)
        {
            AsyncHelpers.RunSync(() => task);
        }

        void InvokeTest(Delegate method, params object[] args)
        {
            if (InvokeRequired)
                Invoke(method, args);
            else
                method.DynamicInvoke(args);
        }

        void InvokeTest(Delegate method)
        {
            if (InvokeRequired)
                Invoke(method);
            else
                method.DynamicInvoke();
        }

        public delegate void ExpressionNoArg();

        void InvokeTestExpression(ExpressionNoArg expr)
        {
            InvokeTest(expr);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(() =>
            {
                Task<LoginResult> res = RobloxBot.Login(textBox1.Text, textBox2.Text);
                while (!res.IsCompleted)
                    Thread.Sleep(1);
                loginResult = res.Result;
                InvokeTestExpression(() => label3.Text = String.Format("{0}", loginResult.FailureReason));
                bot = loginResult.Bot;

                
                RobloxPrivateMessage pm = bot.CreatePrivateMessage();

                pm.RecipientId = 5762824;
                pm.Body = String.Format("Your rank in {0} is {1} ({2})", nrank.GroupID, nrank.Name, nrank.Rank);
                pm.Subject = "Test";
                Task<SendResult_t> resa = pm.Send();
                while (!resa.IsCompleted)
                    Thread.Sleep(1);
                Console.WriteLine("Reason: {0} Short Message: {1} Success: {2}", resa.Result.Result, resa.Result.shortMessage, resa.Result.success);



                Task<PrivateMessage[]> btask = bot.GetMessages();
                while (!btask.IsCompleted)
                    Thread.Sleep(1);

                foreach (PrivateMessage message in btask.Result)
                {
                    Console.WriteLine("Subject: {0} Body: {1} Sent By: {2}", message.Subject, message.Body, message.Sender);
                }
                Console.WriteLine("Get Messages");
                
            });
            t.Start();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(bot != null)
                bot.Dispose();
        }

        GroupRank nrank;

        private void Form1_Load(object sender, EventArgs e)
        {
            Thread t = new Thread(() =>
            {
                Task<GroupRank[]> task = RobloxAPI.GetGroupRanks(1237888);

                while (!task.IsCompleted)
                    Thread.Sleep(1);

                Console.WriteLine("Group Ranks in 1237888.");
                foreach(GroupRank rank in task.Result)
                {
                    Console.WriteLine("ID: {0} Name: {1} Rank: {2}", rank.ID, rank.Name, rank.Rank);
                }
                
                Task<GroupRank> ntask = RobloxAPI.GetUserRankInGroup(5762824, 1237888);

                while (!ntask.IsCompleted)
                    Thread.Sleep(1);

                nrank = ntask.Result;
                if(nrank != null)
                    Console.WriteLine("User 5762824 is GroupRank: ID: {0} Name: {1} Rank: {2}", nrank.ID, nrank.Name, nrank.Rank);
            });
            t.Start();
        }

        T SynchronousAWait<T>(Task<T> task)
        {
            while (!task.IsCompleted)
                Thread.Sleep(1);
            return task.Result;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(bot != null)
            {
                int outint = 0;
                if (int.TryParse(textBox3.Text, out outint))
                {
                    Thread t = new Thread(() =>
                    {
                        GenericResult_t res = SynchronousAWait(bot.RequestFriendship(outint));
                        MessageBox.Show(String.Format("Message: {0} Result: {1} Success: {2}", res.Message, res.ResultEnum, res.Success));
                    });
                    t.Start();
                }
                else
                    MessageBox.Show("Text must be a integer.");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (bot != null)
            {
                int outint = 0;
                if (int.TryParse(textBox3.Text, out outint))
                {
                    Thread t = new Thread(() =>
                    {
                        GenericResult_t res = SynchronousAWait(bot.Unfriend(outint));
                        MessageBox.Show(String.Format("Unfriend Result: Message: {0} Result: {1} Success: {2}", res.Message, res.ResultEnum, res.Success));
                    });
                    t.Start();
                }
                else
                    MessageBox.Show("Text must be a integer.");
            }
        }

        private void openWebBrowserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AwesomiumBrowser browser = new AwesomiumBrowser();
            browser.Show();
        }
    }
}

using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace billing
{
    public class BillingService : Billing.BillingBase
    {
        public static myMoney _users;
        public override async Task ListUsers(None request, IServerStreamWriter<UserProfile> responseStream, ServerCallContext context)
        {
            foreach (var us in _users.usrt)
            {
                await responseStream.WriteAsync(new UserProfile { Amount = us.amount, Name = us.name });
            }
        }
        public override Task<Response> CoinsEmission(EmissionAmount request, ServerCallContext context)
        {
            if (request.Amount < _users.usrt.Count)
            {
                return Task.FromResult(new Response { Status = Response.Types.Status.Failed });
            }
            long smrayting = 0; //сумма рейтингов
            foreach (var us in _users.usrt) { smrayting += us.rayting; }
            List<UserRayting> users = _users.usrt.OrderBy(n => n.rayting).ToList(); //список пользователей отсортированный по рейтингу
            int smam = 0; //сумма начисленного
            for (int ck = 0; ck < users.Count; ck++)
            {
                //начислено
                int am = (int)(request.Amount * users[ck].rayting / smrayting);
                if (am == 0) { am = 1; }
                smam += am;
                //корректировка последнего начисления
                if (ck == users.Count - 1)
                {
                    am = am + (int)(request.Amount - smam);
                }
                users[ck].amount = users[ck].amount + am;
                //добавляем в coin
                int lid = 1;//новый ID
                if (_users.coin.Count != 0)
                {
                    lid = (int)(_users.coin.Max(p => p == null ? 0 : p.id)) + 1;
                }
                _users.coin.Add(new coinmv() { id = lid, dst = users[ck].name, amount = am });
            }
            return Task.FromResult(new Response { Status = Response.Types.Status.Ok });
        }
        public override Task<Response> MoveCoins(MoveCoinsTransaction request, ServerCallContext context)
        {
            //ищем пользователя и сумму
            var usr = _users.usrt.Find(item => item.name == request.SrcUser);
            if (usr == null)
            {
                return Task.FromResult(new Response { Status = Response.Types.Status.Failed });
            }
            //количество денег
            if (request.Amount > usr.amount)
            {
                return Task.FromResult(new Response { Status = Response.Types.Status.Failed });
            }
            //ищем монету
            var coin = _users.coin.Find(item => item.dst == request.SrcUser && item.amount >= request.Amount);
            if (coin == null)
            {
                return Task.FromResult(new Response { Status = Response.Types.Status.Unspecified });
            }
            //ищем получателя
            var dst = _users.usrt.Find(item => item.name == request.DstUser);
            if (dst == null)
            {
                return Task.FromResult(new Response { Status = Response.Types.Status.Failed });
            }
            //правим количество денег
            usr.amount = (int)usr.amount - (int)request.Amount;
            dst.amount = (int)dst.amount + (int)request.Amount;
            //записываем движение монет
            _users.coin.Add(new coinmv() { id = coin.id, dst = dst.name, src = usr.name, amount = (int)request.Amount });
            return Task.FromResult(new Response { Status = Response.Types.Status.Ok });
        }
        public override Task<Coin> LongestHistoryCoin(None request, ServerCallContext context)
        {
            var rez = _users.coin.GroupBy(x => x.id).Select(r => new { id = r.Key, cn = r.Count(), history = r.Select(s => s.dst) }).OrderByDescending(d => d.cn).ToList();
            int _id = 0; string _history = ""; //нач.значения
            if (rez.Count != 0)
            {
                _id = rez[0].id; 
                _history = String.Join(" ", rez[0].history);
            }
            return Task.FromResult(new Coin { Id = _id, History = _history });
        }

    }
    public class myMoney
    {
        public List<coinmv> coin = new List<coinmv>();
        //public List<MoveCoinsTransaction> mvcoin = new List<MoveCoinsTransaction>();
        public List<UserRayting> usrt;
        public List<UserRayting> rtu()
        {
            List<UserRayting> users = new List<UserRayting>() {
                new UserRayting() { name="boris",rayting=5000,amount=0},
                new UserRayting() { name="maria",rayting=1000,amount=0},
                new UserRayting() { name="oleg",rayting=800,amount=0}
            };
            return users;
        }
    }
    public class UserRayting
    {
        public string name { get; set; }
        public int rayting { get; set; }
        public int amount { get; set; }
    }
    public class coinmv
    {
        public string dst { get; set; }
        public string src { get; set; }
        public int amount { get; set; }
        public int id { get; set; }
    }
}

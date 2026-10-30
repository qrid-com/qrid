using Qrs.Tunnel.Shared.Models;
using System;

namespace Qrs.Tunnel.Server.Models
{
    public class TunnelSessionItem
    {
        public string PublicKey { get; set; }
        public DateTime CreateDate { get; set; }
        public DirectionItem DirectionMaToCa { get; set; }
        public DirectionItem DirectionCaToMa { get; set; }

        public DirectionItem GetSendingDirection(string requestDirection)
        {
            return requestDirection == Directions.MA2CA ? DirectionMaToCa : DirectionCaToMa;
        }

        public DirectionItem GetReceivingDirection(string requestDirection)
        {
            return requestDirection == Directions.MA2CA ? DirectionCaToMa : DirectionMaToCa;
        }



        public TunnelSessionItem(string publicKey)
        {
            PublicKey = publicKey;
            CreateDate = DateTime.Now;
            DirectionCaToMa = new DirectionItem(Directions.CA2MA);
            DirectionMaToCa = new DirectionItem(Directions.MA2CA);
        }
        
    }
}
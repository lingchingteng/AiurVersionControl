﻿using AiurVersionControl.Models;

namespace AiurVersionControl.Tests.Models
{
    public class NumberWorkSpace : WorkSpace
    {
        public int NumberStore { get; set; }

        public override WorkSpace Clone()
        {
            return new NumberWorkSpace
            {
                NumberStore = this.NumberStore
            };
        }
    }
}

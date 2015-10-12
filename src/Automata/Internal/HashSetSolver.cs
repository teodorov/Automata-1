﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Automata.Internal
{
    internal class HashSetSolver : ICharAlgebra<HashSet<char>>
    {
        private HashSet<char> sigma_;
        BitWidth encoding;
        MintermGenerator<HashSet<char>> mtg;

        public BitWidth Encoding
        {
            get { return encoding; }
        }
        char minCharacter = (char)0;
        char maxCharacter;

        public HashSetSolver(BitWidth encoding)
        {
            this.encoding = encoding;
            this.maxCharacter = (encoding == BitWidth.BV7 ? '\x007F' :
                (encoding == BitWidth.BV8 ? '\x00FF' : '\xFFFF'));
            sigma_ = new HashSet<char>();
            for (char i = this.minCharacter; i < this.maxCharacter; i++)
            {
                sigma_.Add(i);
            }
            sigma_.Add(this.maxCharacter);
            mtg = new MintermGenerator<HashSet<char>>(this);
        }


        public HashSet<char> MkOr(HashSet<char> constraint1, HashSet<char> constraint2)
        {
            var res = new HashSet<char>(constraint1);
            res.UnionWith(constraint2);
            return res;
        }

        public HashSet<char> MkOr(IEnumerable<HashSet<char>> constraints)
        {
            var res = new HashSet<char>();
            foreach (HashSet<char> cur in constraints)
            {
                res.UnionWith(cur);
            }
            return res;
        }

        public HashSet<char> MkAnd(HashSet<char> constraint1, HashSet<char> constraint2)
        {
            var res = new HashSet<char>(constraint1);
            res.IntersectWith(constraint2);
            return res;
        }

        public HashSet<char> MkAnd(IEnumerable<HashSet<char>> constraints)
        {
            HashSet<char> res = null;

            foreach (HashSet<char> cur in constraints)
            {
                if (res == null)
                {
                    res = new HashSet<char>(cur);
                    continue;
                }

                res.IntersectWith(cur);
                if (res.Count == 0)
                {
                    break;
                }
            }
            return res;
        }


        public HashSet<char> MkAnd(params HashSet<char>[] constraints)
        {
            HashSet<char> res = null;

            foreach (HashSet<char> cur in constraints)
            {
                if (res == null)
                {
                    res = new HashSet<char>(cur);
                    continue;
                }

                res.IntersectWith(cur);
                if (res.Count == 0)
                {
                    break;
                }
            }
            return res;
        }

        public HashSet<char> MkNot(HashSet<char> constraint)
        {
            var res = new HashSet<char>(sigma_);

            res.ExceptWith(constraint);

            return res;
        }

        public HashSet<char> Simplify(HashSet<char> constraint)
        {
            return constraint;
        }
         
        public HashSet<char> True
        {
            get
            {
                
                return sigma_; 
            }
        }

        public HashSet<char> False
        {
            get { return new HashSet<char>();  }
        }

        public HashSet<char> MkRangeConstraint(bool caseInsensitive, char lower, char upper)
        {
            var res = new HashSet<char>();

            // Assumption: [lower, higher] does not cross a case boundary; i.e. all elements
            // in the range are either upper or lower case. 
            //TBD: this is not always true
            if (caseInsensitive)
            {
                for (char i = System.Char.ToUpper(lower); i <= System.Char.ToUpper(upper); ++i)
                {
                    res.Add(i);
                }
                for (char i = System.Char.ToLower(lower); i <= System.Char.ToLower(upper); ++i)
                {
                    res.Add(i);
                }
            }
            else
            {
                for (char i = lower; i <= upper; i++)
                {
                    res.Add(i);
                }
            }
            return res;
        }

        public HashSet<char> MkCharConstraint(bool caseInsensitive, char c)
        {
            var res = new HashSet<char>();
            if (caseInsensitive)
            {
                res.Add(System.Char.ToUpper(c));
                res.Add(System.Char.ToLower(c));
            }
            else
            {
                res.Add(c);
            }
            return res;
        }

        public HashSet<char> MkRangesConstraint(bool caseInsensitive, IEnumerable<char[]> ranges)
        {
            // Assume: all the char[] in ranges have 2 elements exactly
            
            var res = new HashSet<char>();

            foreach(char [] range in ranges)
            {
                char lower = range[0];
                char upper = range[1];
                res.UnionWith(this.MkRangeConstraint(caseInsensitive, lower, upper));
            }
            return res;
        }

        public bool IsSatisfiable(HashSet<char> constraint)
        {
            return constraint.Count > 0;
        }

        public bool AreEquivalent(HashSet<char> constraint1, HashSet<char> constraint2)
        {
            return constraint1.SetEquals(constraint2);
        }

        public IEnumerable<Pair<bool[], HashSet<char>>> GenerateMinterms(HashSet<char>[] constraints)
        {
            return mtg.GenerateMinterms(constraints);
        }

        /*
         * original specialized implementation of minterms
         * 
        public IEnumerable<Pair<bool[], HashSet<char>>> GenerateMinterms(HashSet<char>[] constraints)
        {
            if (constraints.Length == 0)
            {
                yield return new Pair<bool[], HashSet<char>>(new bool[] { }, this.True);
            }
            else
            {
                var mt = new Internal.Minterms<wrapwrap2>(new wrapwrap2(this, this.True));

                var seq = mt.GenerateCombinations(true, Array.ConvertAll(constraints, cur => new wrapwrap2(this, cur)));

                foreach (var pair in seq)
                {
                    yield return new Pair<bool[], HashSet<char>>(pair.First, pair.Second._contents);
                }
            }
        }

        class wrapwrap2 : Internal.ICapNeg
        {
            private HashSetSolver _parent;
            internal HashSet<char> _contents;
            internal HashSet<char> _inverse;

            public wrapwrap2(HashSetSolver parent, HashSet<char> wrapee)
            {
                _parent = parent;
                _contents = wrapee;
                _inverse = null;
            }

            public Internal.ICapNeg cap(Internal.ICapNeg b)
            {
                return new wrapwrap2(_parent, _parent.MkAnd(_contents, ((wrapwrap2)b)._contents));
            }

            public Internal.ICapNeg cup(Internal.ICapNeg b)
            {
                return new wrapwrap2(_parent, _parent.MkOr(_contents, ((wrapwrap2)b)._contents));
            }

            public Internal.ICapNeg minus(Internal.ICapNeg b)
            {
                var bprime = (wrapwrap2)b;

                if (bprime._inverse == null)
                {
                    bprime._inverse = _parent.MkNot(bprime._contents);
                }
                return new wrapwrap2(_parent, _parent.MkAnd(_contents, bprime._inverse));
            }

            public bool same_elts(Internal.ICapNeg b)
            {
                return _contents.SetEquals(((wrapwrap2)b)._contents);
            }

            public bool is_empty()
            {
                return _contents.Count == 0;
            }
        }


        */


        #region IPrettyPrinter<HashSet<char>> Members


        public string PrettyPrint(HashSet<char> pred)
        {
            throw new NotImplementedException();
        }

        #endregion


        #region ICharSolver<HashSet<char>> Members


        public HashSet<char> ConvertFromCharSet(BDD set)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region ICharSolver<HashSet<char>> Members


        public CharSetSolver CharSetProvider
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        #region ICharSolverPred<HashSet<char>> Members

        public HashSet<char> MkCharPredicate(string name, HashSet<char> pred)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IPrettyPrinter<HashSet<char>> Members


        public string PrettyPrint(HashSet<char> t, Func<HashSet<char>, string> varLookup)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IPrettyPrinter<HashSet<char>> Members


        public string PrettyPrintCS(HashSet<char> t, Func<HashSet<char>, string> varLookup)
        {
            throw new NotImplementedException();
        }

        #endregion


        public bool TryConvertToCharSet(HashSet<char> pred, out BDD set)
        {
            set = null;
            return false;
        }

        public HashSet<char> MkSet(uint e)
        {
            throw new NotImplementedException();
        }

        public uint Choose(HashSet<char> s)
        {
            throw new NotImplementedException();
        }
    }
}
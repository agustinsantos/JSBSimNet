#region Copyright(C)  Licensed under GNU GPL.
/// Copyright (C) 2005-2006 Agustin Santos Mendez
/// 
/// JSBSim was developed by Jon S. Berndt, Tony Peden, and
/// David Megginson. 
/// Agustin Santos Mendez implemented and maintains this C# version.
/// 
/// This program is free software; you can redistribute it and/or
///  modify it under the terms of the GNU General Public License
/// as published by the Free Software Foundation; either version 2
/// of the License, or (at your option) any later version.
///  
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
/// GNU General Public License for more details.
///  
/// You should have received a copy of the GNU General Public License
/// along with this program; if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
#endregion
namespace CommonUtils.Tests
{
    using System;

    using NUnit.Framework;

    using CommonUtils.Collections;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections;

    /// <summary>
    /// // Check double-ended queue (deque) collection.
    /// </summary>
    [TestFixture]
    public class DequeTests
    {

        [Test]
        public void Capacity_SetTo0_ActsLikeList()
        {
            var list = new List<int>();
            list.Capacity = 0;
            Assert.AreEqual(0, list.Capacity);

            var deque = new Deque<int>();
            deque.Capacity = 0;
            Assert.AreEqual(0, deque.Capacity);
        }

        [Test]
        public void Capacity_SetNegative_ActsLikeList()
        {
            var list = new List<int>();
            Assert.Throws<ArgumentOutOfRangeException>(() => { list.Capacity = -1; }, "value");

            var deque = new Deque<int>();
            Assert.Throws<ArgumentOutOfRangeException>(() => { deque.Capacity = -1; }, "value");
        }

        [Test]
        public void Capacity_SetLarger_UsesSpecifiedCapacity()
        {
            var deque = new Deque<int>(1);
            Assert.AreEqual(1, deque.Capacity);
            deque.Capacity = 17;
            Assert.AreEqual(17, deque.Capacity);
        }

        [Test]
        public void Capacity_SetSmaller_UsesSpecifiedCapacity()
        {
            var deque = new Deque<int>(13);
            Assert.AreEqual(13, deque.Capacity);
            deque.Capacity = 7;
            Assert.AreEqual(7, deque.Capacity);
        }

        [Test]
        public void Capacity_Set_PreservesData()
        {
            var deque = new Deque<int>(new int[] { 1, 2, 3 });
            Assert.AreEqual(3, deque.Capacity);
            deque.Capacity = 7;
            Assert.AreEqual(7, deque.Capacity);
            Assert.AreEqual(new[] { 1, 2, 3 }, deque);
        }

        [Test]
        public void Capacity_Set_WhenSplit_PreservesData()
        {
            var deque = new Deque<int>(new int[] { 1, 2, 3 });
            deque.RemoveFromFront();
            deque.AddToBack(4);
            Assert.AreEqual(3, deque.Capacity);
            deque.Capacity = 7;
            Assert.AreEqual(7, deque.Capacity);
            Assert.AreEqual(new[] { 2, 3, 4 }, deque);
        }

        [Test]
        public void Capacity_Set_SmallerThanCount_ActsLikeList()
        {
            var list = new List<int>(new int[] { 1, 2, 3 });
            Assert.AreEqual(3, list.Capacity);
            Assert.Throws<ArgumentOutOfRangeException>(() => { list.Capacity = 2; }, "value");

            var deque = new Deque<int>(new int[] { 1, 2, 3 });
            Assert.AreEqual(3, deque.Capacity);
            Assert.Throws<ArgumentOutOfRangeException>(() => { deque.Capacity = 2; }, "value");
        }

        [Test]
        public void Capacity_Set_ToItself_DoesNothing()
        {
            var deque = new Deque<int>(13);
            Assert.AreEqual(13, deque.Capacity);
            deque.Capacity = 13;
            Assert.AreEqual(13, deque.Capacity);
        }

        // Implementation detail: the default capacity.
        const int DefaultCapacity = 8;

        [Test]
        public void Constructor_WithoutExplicitCapacity_UsesDefaultCapacity()
        {
            var deque = new Deque<int>();
            Assert.AreEqual(DefaultCapacity, deque.Capacity);
        }

        [Test]
        public void Constructor_CapacityOf0_ActsLikeList()
        {
            var list = new List<int>(0);
            Assert.AreEqual(0, list.Capacity);

            var deque = new Deque<int>(0);
            Assert.AreEqual(0, deque.Capacity);
        }

        [Test]
        public void Constructor_CapacityOf0_PermitsAdd()
        {
            var deque = new Deque<int>(0);
            deque.AddToBack(13);
            Assert.AreEqual(new[] { 13 }, deque);
        }

        [Test]
        public void Constructor_NegativeCapacity_ActsLikeList()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new List<int>(-1), "capacity");

            Assert.Throws<ArgumentOutOfRangeException>(() => new Deque<int>(-1), "capacity");
        }

        [Test]
        public void Constructor_CapacityOf1_UsesSpecifiedCapacity()
        {
            var deque = new Deque<int>(1);
            Assert.AreEqual(1, deque.Capacity);
        }

        [Test]
        public void Constructor_FromEmptySequence_UsesDefaultCapacity()
        {
            var deque = new Deque<int>(new int[] { });
            Assert.AreEqual(DefaultCapacity, deque.Capacity);
        }

        [Test]
        public void Constructor_FromSequence_InitializesFromSequence()
        {
            var deque = new Deque<int>(new int[] { 1, 2, 3 });
            Assert.AreEqual(3, deque.Capacity);
            Assert.AreEqual(3, deque.Count);
            Assert.AreEqual(new int[] { 1, 2, 3 }, deque);
        }

        [Test]
        public void IndexOf_ItemPresent_ReturnsItemIndex()
        {
            var deque = new Deque<int>(new[] { 1, 2 });
            var result = deque.IndexOf(2);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void IndexOf_ItemNotPresent_ReturnsNegativeOne()
        {
            var deque = new Deque<int>(new[] { 1, 2 });
            var result = deque.IndexOf(3);
            Assert.AreEqual(-1, result);
        }

        [Test]
        public void IndexOf_ItemPresentAndSplit_ReturnsItemIndex()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            deque.RemoveFromBack();
            deque.AddToFront(0);
            Assert.AreEqual(0, deque.IndexOf(0));
            Assert.AreEqual(1, deque.IndexOf(1));
            Assert.AreEqual(2, deque.IndexOf(2));
        }

        [Test]
        public void Contains_ItemPresent_ReturnsTrue()
        {
            var deque = new Deque<int>(new[] { 1, 2 }) as ICollection<int>;
            Assert.True(deque.Contains(2));
        }

        [Test]
        public void Contains_ItemNotPresent_ReturnsFalse()
        {
            var deque = new Deque<int>(new[] { 1, 2 }) as ICollection<int>;
            Assert.False(deque.Contains(3));
        }

        [Test]
        public void Contains_ItemPresentAndSplit_ReturnsTrue()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            deque.RemoveFromBack();
            deque.AddToFront(0);
            var deq = deque as ICollection<int>;
            Assert.True(deq.Contains(0));
            Assert.True(deq.Contains(1));
            Assert.True(deq.Contains(2));
            Assert.False(deq.Contains(3));
        }

        [Test]
        public void Add_IsAddToBack()
        {
            var deque1 = new Deque<int>(new[] { 1, 2 });
            var deque2 = new Deque<int>(new[] { 1, 2 });
            ((ICollection<int>)deque1).Add(3);
            deque2.AddToBack(3);
            Assert.AreEqual(deque1, deque2);
        }

        [Test]
        public void NonGenericEnumerator_EnumeratesItems()
        {
            var deque = new Deque<int>(new[] { 1, 2 });
            var results = new List<int>();
            var objEnum = ((System.Collections.IEnumerable)deque).GetEnumerator();
            while (objEnum.MoveNext())
            {
                results.Add((int)objEnum.Current);
            }
            Assert.AreEqual(results, deque);
        }

        [Test]
        public void IsReadOnly_ReturnsFalse()
        {
            var deque = new Deque<int>();
            Assert.False(((ICollection<int>)deque).IsReadOnly);
        }

        [Test]
        public void CopyTo_CopiesItems()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            var results = new int[3];
            ((ICollection<int>)deque).CopyTo(results, 0);
        }

        [Test]
        public void CopyTo_NullArray_ActsLikeList()
        {
            var list = new List<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentNullException>(() => ((ICollection<int>)list).CopyTo(null, 0));

            var deque = new Deque<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentNullException>(() => ((ICollection<int>)deque).CopyTo(null, 0));
        }

        [Test]
        public void CopyTo_NegativeOffset_ActsLikeList()
        {
            var destination = new int[3];
            var list = new List<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentOutOfRangeException>(() => ((ICollection<int>)list).CopyTo(destination, -1));

            var deque = new Deque<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentOutOfRangeException>(() => ((ICollection<int>)deque).CopyTo(destination, -1));
        }

        [Test]
        public void CopyTo_InsufficientSpace_ActsLikeList()
        {
            var destination = new int[3];
            var list = new List<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentException>(() => ((ICollection<int>)list).CopyTo(destination, 1));

            var deque = new Deque<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentException>(() => ((ICollection<int>)deque).CopyTo(destination, 1));
        }

        [Test]
        public void Clear_EmptiesAllItems()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            deque.Clear();
            Assert.AreEqual(0, deque.Count);
            Assert.AreEqual(new int[] { }, deque);
        }

        [Test]
        public void Clear_DoesNotChangeCapacity()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            Assert.AreEqual(3, deque.Capacity);
            deque.Clear();
            Assert.AreEqual(3, deque.Capacity);
        }

        [Test]
        public void RemoveFromFront_Empty_ActsLikeStack()
        {
            var stack = new Stack<int>();
            Assert.Throws<InvalidOperationException>(() => stack.Pop());

            var deque = new Deque<int>();
            Assert.Throws<InvalidOperationException>(() => deque.RemoveFromFront());
        }

        [Test]
        public void RemoveFromBack_Empty_ActsLikeQueue()
        {
            var queue = new Queue<int>();
            Assert.Throws<InvalidOperationException>(() => queue.Dequeue());

            var deque = new Deque<int>();
            Assert.Throws<InvalidOperationException>(() => deque.RemoveFromBack());
        }

        [Test]
        public void Remove_ItemPresent_RemovesItemAndReturnsTrue()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3, 4 });
            var result = deque.Remove(3);
            Assert.True(result);
            Assert.AreEqual(new[] { 1, 2, 4 }, deque);
        }

        [Test]
        public void Remove_ItemNotPresent_KeepsItemsReturnsFalse()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3, 4 });
            var result = deque.Remove(5);
            Assert.False(result);
            Assert.AreEqual(new[] { 1, 2, 3, 4 }, deque);
        }

        [Test]
        public void Insert_InsertsElementAtIndex()
        {
            var deque = new Deque<int>(new[] { 1, 2 });
            deque.Insert(1, 13);
            Assert.AreEqual(new[] { 1, 13, 2 }, deque);
        }

        [Test]
        public void Insert_AtIndex0_IsSameAsAddToFront()
        {
            var deque1 = new Deque<int>(new[] { 1, 2 });
            var deque2 = new Deque<int>(new[] { 1, 2 });
            deque1.Insert(0, 0);
            deque2.AddToFront(0);
            Assert.AreEqual(deque1, deque2);
        }

        [Test]
        public void Insert_AtCount_IsSameAsAddToBack()
        {
            var deque1 = new Deque<int>(new[] { 1, 2 });
            var deque2 = new Deque<int>(new[] { 1, 2 });
            deque1.Insert(deque1.Count, 0);
            deque2.AddToBack(0);
            Assert.AreEqual(deque1, deque2);
        }

        [Test]
        public void Insert_NegativeIndex_ActsLikeList()
        {
            var list = new List<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, 0), "index");

            var deque = new Deque<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentOutOfRangeException>(() => deque.Insert(-1, 0), "index");
        }

        [Test]
        public void Insert_IndexTooLarge_ActsLikeList()
        {
            var list = new List<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(list.Count + 1, 0), "index");

            var deque = new Deque<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentOutOfRangeException>(() => deque.Insert(deque.Count + 1, 0), "index");
        }

        [Test]
        public void RemoveAt_RemovesElementAtIndex()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            deque.RemoveFromBack();
            deque.AddToFront(0);
            deque.RemoveAt(1);
            Assert.AreEqual(new[] { 0, 2 }, deque);
        }

        [Test]
        public void RemoveAt_Index0_IsSameAsRemoveFromFront()
        {
            var deque1 = new Deque<int>(new[] { 1, 2 });
            var deque2 = new Deque<int>(new[] { 1, 2 });
            deque1.RemoveAt(0);
            deque2.RemoveFromFront();
            Assert.AreEqual(deque1, deque2);
        }

        [Test]
        public void RemoveAt_LastIndex_IsSameAsRemoveFromBack()
        {
            var deque1 = new Deque<int>(new[] { 1, 2 });
            var deque2 = new Deque<int>(new[] { 1, 2 });
            deque1.RemoveAt(1);
            deque2.RemoveFromBack();
            Assert.AreEqual(deque1, deque2);
        }

        [Test]
        public void RemoveAt_NegativeIndex_ActsLikeList()
        {
            var list = new List<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1), "index");

            var deque = new Deque<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentOutOfRangeException>(() => deque.RemoveAt(-1), "index");
        }

        [Test]
        public void RemoveAt_IndexTooLarge_ActsLikeList()
        {
            var list = new List<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(list.Count), "index");

            var deque = new Deque<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentOutOfRangeException>(() => deque.RemoveAt(deque.Count), "index");
        }

        [Test]
        public void InsertMultiple()
        {
            InsertTest(new[] { 1, 2, 3 }, new[] { 7, 13 });
            InsertTest(new[] { 1, 2, 3, 4 }, new[] { 7, 13 });
        }

        private void InsertTest(IReadOnlyCollection<int> initial, IReadOnlyCollection<int> items)
        {
            var totalCapacity = initial.Count + items.Count;
            for (int rotated = 0; rotated <= totalCapacity; ++rotated)
            {
                for (int index = 0; index <= initial.Count; ++index)
                {
                    // Calculate the expected result using the slower List<int>.
                    var result = new List<int>(initial);
                    for (int i = 0; i != rotated; ++i)
                    {
                        var item = result[0];
                        result.RemoveAt(0);
                        result.Add(item);
                    }
                    result.InsertRange(index, items);

                    // First, start off the deque with the initial items.
                    var deque = new Deque<int>(initial);

                    // Ensure there's enough room for the inserted items.
                    deque.Capacity += items.Count;

                    // Rotate the existing items.
                    for (int i = 0; i != rotated; ++i)
                    {
                        var item = deque[0];
                        deque.RemoveFromFront();
                        deque.AddToBack(item);
                    }

                    // Do the insert.
                    deque.InsertRange(index, items);

                    // Ensure the results are as expected.
                    Assert.AreEqual(result, deque);
                }
            }
        }

        [Test]
        public void Insert_RangeOfZeroElements_HasNoEffect()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            deque.InsertRange(1, new int[] { });
            Assert.AreEqual(new[] { 1, 2, 3 }, deque);
        }

        [Test]
        public void InsertMultiple_MakesRoomForNewElements()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            deque.InsertRange(1, new[] { 7, 13 });
            Assert.AreEqual(new[] { 1, 7, 13, 2, 3 }, deque);
            Assert.AreEqual(5, deque.Capacity);
        }

        [Test]
        public void RemoveMultiple()
        {
            RemoveTest(new[] { 1, 2, 3 });
            RemoveTest(new[] { 1, 2, 3, 4 });
        }

        private void RemoveTest(IReadOnlyCollection<int> initial)
        {
            for (int count = 0; count <= initial.Count; ++count)
            {
                for (int rotated = 0; rotated <= initial.Count; ++rotated)
                {
                    for (int index = 0; index <= initial.Count - count; ++index)
                    {
                        // Calculated the expected result using the slower List<int>.
                        var result = new List<int>(initial);
                        for (int i = 0; i != rotated; ++i)
                        {
                            var item = result[0];
                            result.RemoveAt(0);
                            result.Add(item);
                        }
                        result.RemoveRange(index, count);

                        // First, start off the deque with the initial items.
                        var deque = new Deque<int>(initial);

                        // Rotate the existing items.
                        for (int i = 0; i != rotated; ++i)
                        {
                            var item = deque[0];
                            deque.RemoveFromFront();
                            deque.AddToBack(item);
                        }

                        // Do the remove.
                        deque.RemoveRange(index, count);

                        // Ensure the results are as expected.
                        Assert.AreEqual(result, deque);
                    }
                }
            }
        }

        [Test]
        public void Remove_RangeOfZeroElements_HasNoEffect()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            deque.RemoveRange(1, 0);
            Assert.AreEqual(new[] { 1, 2, 3 }, deque);
        }

        [Test]
        public void Remove_NegativeCount_ActsLikeList()
        {
            var list = new List<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveRange(1, -1), "count");

            var deque = new Deque<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentOutOfRangeException>(() => deque.RemoveRange(1, -1), "count");
        }

        [Test]
        public void GetItem_ReadsElements()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            Assert.AreEqual(1, deque[0]);
            Assert.AreEqual(2, deque[1]);
            Assert.AreEqual(3, deque[2]);
        }

        [Test]
        public void GetItem_Split_ReadsElements()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            deque.RemoveFromBack();
            deque.AddToFront(0);
            Assert.AreEqual(0, deque[0]);
            Assert.AreEqual(1, deque[1]);
            Assert.AreEqual(2, deque[2]);
        }

        [Test]
        public void GetItem_IndexTooLarge_ActsLikeList()
        {
            var list = new List<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentOutOfRangeException>(() => list[3] = 1, "index");

            var deque = new Deque<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentOutOfRangeException>(() => deque[3] = 1, "index");
        }

        [Test]
        public void GetItem_NegativeIndex_ActsLikeList()
        {
            var list = new List<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentOutOfRangeException>(() => list[-1] = 1, "index");

            var deque = new Deque<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentOutOfRangeException>(() => deque[-1] = 1, "index");
        }

        [Test]
        public void SetItem_WritesElements()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            deque[0] = 7;
            deque[1] = 11;
            deque[2] = 13;
            Assert.AreEqual(new[] { 7, 11, 13 }, deque);
        }

        [Test]
        public void SetItem_Split_WritesElements()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            deque.RemoveFromBack();
            deque.AddToFront(0);
            deque[0] = 7;
            deque[1] = 11;
            deque[2] = 13;
            Assert.AreEqual(new[] { 7, 11, 13 }, deque);
        }

        [Test]
        public void SetItem_IndexTooLarge_ActsLikeList()
        {
            var list = new List<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentOutOfRangeException>(() => { list[3] = 13; }, "index");

            var deque = new Deque<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentOutOfRangeException>(() => { deque[3] = 13; }, "index");
        }

        [Test]
        public void SetItem_NegativeIndex_ActsLikeList()
        {
            var list = new List<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentOutOfRangeException>(() => { list[-1] = 13; }, "index");

            var deque = new Deque<int>(new[] { 1, 2, 3 });
            Assert.Throws<ArgumentOutOfRangeException>(() => { deque[-1] = 13; }, "index");
        }

        [Test]
        public void NongenericIndexOf_ItemPresent_ReturnsItemIndex()
        {
            var deque = new Deque<int>(new[] { 1, 2 }) as IList;
            var result = deque.IndexOf(2);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void NongenericIndexOf_ItemNotPresent_ReturnsNegativeOne()
        {
            var deque = new Deque<int>(new[] { 1, 2 }) as IList;
            var result = deque.IndexOf(3);
            Assert.AreEqual(-1, result);
        }

        [Test]
        public void NongenericIndexOf_ItemPresentAndSplit_ReturnsItemIndex()
        {
            var deque_ = new Deque<int>(new[] { 1, 2, 3 });
            deque_.RemoveFromBack();
            deque_.AddToFront(0);
            var deque = deque_ as IList;
            Assert.AreEqual(0, deque.IndexOf(0));
            Assert.AreEqual(1, deque.IndexOf(1));
            Assert.AreEqual(2, deque.IndexOf(2));
        }

        [Test]
        public void NongenericIndexOf_WrongItemType_ReturnsNegativeOne()
        {
            var list = new List<int>(new[] { 1, 2 }) as IList;
            Assert.AreEqual(-1, list.IndexOf(this));

            var deque = new Deque<int>(new[] { 1, 2 }) as IList;
            Assert.AreEqual(-1, deque.IndexOf(this));
        }

        [Test]
        public void NongenericContains_WrongItemType_ReturnsFalse()
        {
            var list = new List<int>(new[] { 1, 2 }) as IList;
            Assert.False(list.Contains(this));

            var deque = new Deque<int>(new[] { 1, 2 }) as IList;
            Assert.False(deque.Contains(this));
        }

        [Test]
        public void NongenericContains_ItemPresent_ReturnsTrue()
        {
            var deque = new Deque<int>(new[] { 1, 2 }) as IList;
            Assert.True(deque.Contains(2));
        }

        [Test]
        public void NongenericContains_ItemNotPresent_ReturnsFalse()
        {
            var deque = new Deque<int>(new[] { 1, 2 }) as IList;
            Assert.False(deque.Contains(3));
        }

        [Test]
        public void NongenericContains_ItemPresentAndSplit_ReturnsTrue()
        {
            var deque_ = new Deque<int>(new[] { 1, 2, 3 });
            deque_.RemoveFromBack();
            deque_.AddToFront(0);
            var deque = deque_ as IList;
            Assert.True(deque.Contains(0));
            Assert.True(deque.Contains(1));
            Assert.True(deque.Contains(2));
            Assert.False(deque.Contains(3));
        }

        [Test]
        public void NongenericIsReadOnly_ReturnsFalse()
        {
            var deque = new Deque<int>() as IList;
            Assert.False(deque.IsReadOnly);
        }

        [Test]
        public void NongenericCopyTo_CopiesItems()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 }) as IList;
            var results = new int[3];
            deque.CopyTo(results, 0);
        }

        [Test]
        public void NongenericCopyTo_NullArray_ActsLikeList()
        {
            var list = new List<int>(new[] { 1, 2, 3 }) as IList;
            Assert.Throws<ArgumentNullException>(() => list.CopyTo(null, 0));

            var deque = new Deque<int>(new[] { 1, 2, 3 }) as IList;
            Assert.Throws<ArgumentNullException>(() => deque.CopyTo(null, 0));
        }

        [Test]
        public void NongenericCopyTo_NegativeOffset_ActsLikeList()
        {
            var destination = new int[3];
            var list = new List<int>(new[] { 1, 2, 3 }) as IList;
            Assert.Throws<ArgumentOutOfRangeException>(() => list.CopyTo(destination, -1));

            var deque = new Deque<int>(new[] { 1, 2, 3 }) as IList;
            Assert.Throws<ArgumentOutOfRangeException>(() => deque.CopyTo(destination, -1));
        }

        [Test]
        public void NongenericCopyTo_InsufficientSpace_ActsLikeList()
        {
            var destination = new int[3];
            var list = new List<int>(new[] { 1, 2, 3 }) as IList;
            Assert.Throws<ArgumentException>(() => list.CopyTo(destination, 1));

            var deque = new Deque<int>(new[] { 1, 2, 3 }) as IList;
            Assert.Throws<ArgumentException>(() => deque.CopyTo(destination, 1));
        }

        [Test]
        public void NongenericCopyTo_WrongType_ActsLikeList()
        {
            var destination = new IList[3];
            var list = new List<int>(new[] { 1, 2, 3 }) as IList;
            Assert.Throws<ArgumentException>(() => list.CopyTo(destination, 0));

            var deque = new Deque<int>(new[] { 1, 2, 3 }) as IList;
            Assert.Throws<ArgumentException>(() => deque.CopyTo(destination, 0));
        }

        [Test]
        public void NongenericCopyTo_MultidimensionalArray_ActsLikeList()
        {
            var destination = new int[3, 3];
            var list = new List<int>(new[] { 1, 2, 3 }) as IList;
            Assert.Throws<ArgumentException>(() => list.CopyTo(destination, 0));

            var deque = new Deque<int>(new[] { 1, 2, 3 }) as IList;
            Assert.Throws<ArgumentException>(() => deque.CopyTo(destination, 0));
        }

        [Test]
        public void NongenericAdd_WrongType_ActsLikeList()
        {
            var list = new List<int>() as IList;
            Assert.Throws<ArgumentException>(() => list.Add(this), "value");

            var deque = new Deque<int>() as IList;
            Assert.Throws<ArgumentException>(() => deque.Add(this), "value");
        }

        [Test]
        public void NongenericNullableType_AllowsInsertingNull()
        {
            var deque = new Deque<int?>();
            var list = deque as IList;
            var result = list.Add(null);
            Assert.AreEqual(0, result);
            Assert.AreEqual(new int?[] { null }, deque);
        }

        [Test]
        public void NongenericClassType_AllowsInsertingNull()
        {
            var deque = new Deque<object>();
            var list = deque as IList;
            var result = list.Add(null);
            Assert.AreEqual(0, result);
            Assert.AreEqual(new object[] { null }, deque);
        }

        [Test]
        public void NongenericInterfaceType_AllowsInsertingNull()
        {
            var deque = new Deque<IList>();
            var list = deque as IList;
            var result = list.Add(null);
            Assert.AreEqual(0, result);
            Assert.AreEqual(new IList[] { null }, deque);
        }

        [Test]
        public void NongenericStruct_AddNull_ActsLikeList()
        {
            var list = new List<int>() as IList;
            Assert.Throws<ArgumentNullException>(() => list.Add(null));

            var deque = new Deque<int>() as IList;
            Assert.Throws<ArgumentNullException>(() => deque.Add(null));
        }

        [Test]
        public void NongenericGenericStruct_AddNull_ActsLikeList()
        {
            var list = new List<KeyValuePair<int, int>>() as IList;
            Assert.Throws<ArgumentNullException>(() => list.Add(null));

            var deque = new Deque<KeyValuePair<int, int>>() as IList;
            Assert.Throws<ArgumentNullException>(() => deque.Add(null));
        }

        [Test]
        public void NongenericIsFixedSize_IsFalse()
        {
            var deque = new Deque<int>() as IList;
            Assert.False(deque.IsFixedSize);
        }

        [Test]
        public void NongenericIsSynchronized_IsFalse()
        {
            var deque = new Deque<int>() as IList;
            Assert.False(deque.IsSynchronized);
        }

        [Test]
        public void NongenericSyncRoot_IsSelf()
        {
            var deque = new Deque<int>() as IList;
            Assert.AreSame(deque, deque.SyncRoot);
        }

        [Test]
        public void NongenericInsert_InsertsItem()
        {
            var deque = new Deque<int>();
            var list = deque as IList;
            list.Insert(0, 7);
            Assert.AreEqual(new[] { 7 }, deque);
        }

        [Test]
        public void NongenericInsert_WrongType_ActsLikeList()
        {
            var list = new List<int>() as IList;
            Assert.Throws<ArgumentException>(() => list.Insert(0, this), "value");

            var deque = new Deque<int>() as IList;
            Assert.Throws<ArgumentException>(() => deque.Insert(0, this), "value");
        }

        [Test]
        public void NongenericStruct_InsertNull_ActsMostlyLikeList()
        {
            var list = new List<int>() as IList;
            Assert.Throws<ArgumentNullException>(() => list.Insert(0, null), "item"); // Should probably be "value".

            var deque = new Deque<int>() as IList;
            Assert.Throws<ArgumentNullException>(() => deque.Insert(0, null), "value");
        }

        [Test]
        public void NongenericRemove_RemovesItem()
        {
            var deque = new Deque<int>(new[] { 13 });
            var list = deque as IList;
            list.Remove(13);
            Assert.AreEqual(new int[] { }, deque);
        }

        [Test]
        public void NongenericRemove_WrongType_DoesNothing()
        {
            var list = new List<int>(new[] { 13 }) as IList;
            list.Remove(this);
            list.Remove(null);
            Assert.AreEqual(1, list.Count);

            var deque = new Deque<int>(new[] { 13 }) as IList;
            deque.Remove(this);
            deque.Remove(null);
            Assert.AreEqual(1, deque.Count);
        }

        [Test]
        public void NongenericGet_GetsItem()
        {
            var deque = new Deque<int>(new[] { 13 }) as IList;
            var value = (int)deque[0];
            Assert.AreEqual(13, value);
        }

        [Test]
        public void NongenericSet_SetsItem()
        {
            var deque = new Deque<int>(new[] { 13 });
            var list = deque as IList;
            list[0] = 7;
            Assert.AreEqual(new[] { 7 }, deque);
        }

        [Test]
        public void NongenericSet_WrongType_ActsLikeList()
        {
            var list = new List<int>(new[] { 13 }) as IList;
            Assert.Throws<ArgumentException>(() => { list[0] = this; }, "value");

            var deque = new Deque<int>(new[] { 13 }) as IList;
            Assert.Throws<ArgumentException>(() => { deque[0] = this; }, "value");
        }

        [Test]
        public void NongenericStruct_SetNull_ActsLikeList()
        {
            var list = new List<int>(new[] { 13 }) as IList;
            Assert.Throws<ArgumentNullException>(() => { list[0] = null; }, "value");

            var deque = new Deque<int>(new[] { 13 }) as IList;
            Assert.Throws<ArgumentNullException>(() => { deque[0] = null; }, "value");
        }

        [Test]
        public void ToArray_CopiesToNewArray()
        {
            var deque = new Deque<int>(new[] { 0, 1 });
            deque.AddToBack(13);
            var result = deque.ToArray();
            Assert.AreEqual(new[] { 0, 1, 13 }, result);
        }

    }
}

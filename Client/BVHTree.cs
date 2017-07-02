/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Represents a 2D bounding box.
    /// </summary>
    internal struct BVHBox {
        public Vector2 min;
        public Vector2 max;

        /// <summary>
        /// Constructs a new bounding box with explicit coordinates.
        /// </summary>
        /// <param name="min">The minimum (top-left) point.</param>
        /// <param name="max">The maximum (bottom-right) point.</param>
        public BVHBox(Vector2 min, Vector2 max) {
            this.min = min;
            this.max = max;
        }

        /// <summary>
        /// Constructs a new bounding box using an object's position and collision data.
        /// </summary>
        /// <param name="gobj">An object to base the box upon.</param>
        public BVHBox(GameObject gobj) {
            this.min = gobj.Position - new Vector2(gobj.Bounding, gobj.Bounding);
            this.max = gobj.Position + new Vector2(gobj.Bounding, gobj.Bounding);
        }

        /// <summary>
        /// Returns a new bounding box that contains both specified bounding boxes.
        /// </summary>
        public static BVHBox Combine(BVHBox a, BVHBox b) {
            return new BVHBox(Vector2.Min(a.min, b.min), Vector2.Max(a.max, b.max));
        }

        /// <summary>
        /// Returns the square area of this bounding box.
        /// </summary>
        public float Area {
            get {
                Vector2 sides = max - min;
                return sides.X * sides.Y;
            }
        }

        /// <summary>
        /// Returns true if this bounding box intersects with or contains the other bounding box.
        /// </summary>
        /// <param name="other">The other box to test against.</param>
        public bool Intersects(BVHBox other) {
            if (max.X < other.min.X || min.X > other.max.X)
                return false;
            if (max.Y < other.min.Y || min.Y > other.max.Y)
                return false;
            return true;
        }
    }

    /// <summary>
    /// Represents a single node or leaf in a BVH tree.
    /// </summary>
    internal class BVHNode {
        public int height;
        public int next;
        public int child1;
        public int child2;

        public BVHBox shape;
        public GameObject gobj;

        /// <summary>
        /// Returns true if this node does not have any children.
        /// </summary>
        public bool IsLeaf => child1 == BVHTree.NULL_NODE;
    }

    /// <summary>
    /// Represents a bounding volume hierarchy.
    /// </summary>
    internal class BVHTree {

        public const int NULL_NODE = -1;

        private readonly List<BVHNode> nodes;
        private int root;
        private int freeNode;

        /// <summary>
        /// Constructs a new empty tree.
        /// </summary>
        public BVHTree() {
            nodes = new List<BVHNode>();
            ResizeBuffer(16);
            Clear();
        }

        /// <summary>
        /// Clears the hierarchy, removing all objects and nodes.
        /// </summary>
        public void Clear() {
            root = NULL_NODE;
            for (int i = 0; i < nodes.Count - 1; i++) {
                nodes[i].next = i + 1;
                nodes[i].height = -1;
            }
            nodes[nodes.Count - 1].next = NULL_NODE;
            nodes[nodes.Count - 1].height = -1;
            freeNode = 0;
        }

        /// <summary>
        /// Inserts a <seealso cref="GameObject"/> into the hierarchy.
        /// </summary>
        /// <param name="gobj">The object to insert.</param>
        public void Insert(GameObject gobj) {
            int insert = AllocateNode();
            nodes[insert].shape = new BVHBox(gobj);
            nodes[insert].gobj = gobj;
            nodes[insert].height = 0;

            if (root == NULL_NODE) {
                root = insert;
                nodes[root].next = NULL_NODE;
                return;
            }

            BVHBox insertShape = nodes[insert].shape;
            int index = root;
            while (!nodes[index].IsLeaf) {
                int child1 = nodes[index].child1;
                int child2 = nodes[index].child2;
                float area = nodes[index].shape.Area;

                BVHBox combinedShape = BVHBox.Combine(nodes[index].shape, insertShape);
                float combinedArea = combinedShape.Area;

                // cost of creating a new parent for this node and the new leaf
                float cost = 2.0f * combinedArea;
                // minimum cost of pushing the leaf further down the tree
                float inheritanceCost = 2.0f * (combinedArea - area);
                // cost of descending into each child node respectively
                float cost1 = ComputeDescendCost(nodes[child1], insertShape, inheritanceCost);
                float cost2 = ComputeDescendCost(nodes[child2], insertShape, inheritanceCost);
                // pick the best option
                if (cost < cost1 && cost < cost2) break;
                index = cost1 < cost2 ? child1 : child2;
            }

            // create a new parent
            int oldParent = nodes[index].next;
            int newParent = AllocateNode();
            nodes[newParent].next = oldParent;
            nodes[newParent].shape = BVHBox.Combine(insertShape, nodes[index].shape);
            nodes[newParent].height = nodes[index].height + 1;

            if (oldParent != NULL_NODE) {
                // index was not the root
                if (nodes[oldParent].child1 == index) {
                    nodes[oldParent].child1 = newParent;
                } else {
                    nodes[oldParent].child2 = newParent;
                }

                nodes[newParent].child1 = index;
                nodes[newParent].child2 = insert;
                nodes[index].next = newParent;
                nodes[insert].next = newParent;
            } else {
                // index was the root
                nodes[newParent].child1 = index;
                nodes[newParent].child2 = insert;
                nodes[index].next = newParent;
                nodes[insert].next = newParent;
                root = newParent;
            }

            // Walk back up the tree fixing the shapes and the heights
            index = nodes[insert].next;
            while (index != NULL_NODE) {
                index = Balance(index);

                int child1 = nodes[index].child1;
                int child2 = nodes[index].child2;
                Debug.Assert(child1 != NULL_NODE);
                Debug.Assert(child2 != NULL_NODE);

                nodes[index].height = 1 + MathHelper.Max(nodes[child1].height, nodes[child2].height);
                nodes[index].shape = BVHBox.Combine(nodes[child1].shape, nodes[child2].shape);

                index = nodes[index].next;
            }
        }

        /// <summary>
        /// Returns a list of <seealso cref="GameObject"/>s that are potentially colliding with the given bounding box.
        /// </summary>
        /// <param name="shape">A bounding box to query against the hierarchy.</param>
        public List<GameObject> Query(BVHBox shape) {
            List<GameObject> result = new List<GameObject>();
            Stack<int> stack = new Stack<int>();
            stack.Push(root);

            while (stack.Count > 0) {
                int node = stack.Pop();
                if (node == NULL_NODE) continue;

                BVHNode n = nodes[node];
                if (!n.shape.Intersects(shape)) continue;

                if (n.IsLeaf) {
                    result.Add(n.gobj);
                } else {
                    stack.Push(n.child1);
                    stack.Push(n.child2);
                }
            }

            return result;
        }

#if DEBUG
        /// <summary>
        /// Draws the tree to the screen.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, Vector2 viewMin, Vector2 viewMax, Vector2 viewport, int level = -1) {
            for (var i = 0; i < freeNode; i++) {
                BVHNode node = nodes[i];
                bool leaf = node.IsLeaf;
                bool h = node.height == level ||
                    (node.next < nodes.Count && node.next != NULL_NODE && nodes[node.next].height - 1 == level);
                if (!h && !leaf && level >= 0) continue;

                Vector2 smin = Utils.CalculateScreenPos(viewMin, viewMax, viewport, node.shape.min);
                Vector2 smax = Utils.CalculateScreenPos(viewMin, viewMax, viewport, node.shape.max);
                spriteBatch.DrawBorder(Assets.GetTexture("ui/outlinerect"), new Rectangle((int)smin.X, (int)smin.Y, (int)(smax.X - smin.X), (int)(smax.Y - smin.Y)),
                    Color.FromNonPremultiplied(h ? 255 : 0, leaf ? 255 : 0, 255, 255), 16);
            }
        }
#endif

        /// <summary>
        /// Calculates the cost of descending the target shape into the specified node.
        /// </summary>
        /// <param name="node">The node to calculate cost for.</param>
        /// <param name="targetShape">The shape attempting to descend into the node.</param>
        /// <param name="inheritanceCost">Base cost of descending.</param>
        private float ComputeDescendCost(BVHNode node, BVHBox targetShape, float inheritanceCost) {
            if (node.IsLeaf) {
                BVHBox shape = BVHBox.Combine(targetShape, node.shape);
                return shape.Area + inheritanceCost;
            } else {
                BVHBox shape = BVHBox.Combine(targetShape, node.shape);
                float oldArea = node.shape.Area;
                float newArea = shape.Area;
                return newArea - oldArea + inheritanceCost;
            }
        }

        /// <summary>
        /// Resizes the internal node array.
        /// </summary>
        /// <param name="newCapacity">The array's new capacity.</param>
        private void ResizeBuffer(int newCapacity) {
            nodes.Capacity = newCapacity;
            for (int i = nodes.Count; i < newCapacity; i++) {
                nodes.Add(new BVHNode {
                    next = i + 1,
                    height = -1
                });
            }
            nodes[nodes.Count - 1].next = NULL_NODE;
        }

        /// <summary>
        /// Reserves a node in the array and returns its index.
        /// </summary>
        private int AllocateNode() {
            // resize the buffer
            if (freeNode == NULL_NODE) {
                freeNode = nodes.Count;
                ResizeBuffer(nodes.Count * 2);
            }

            // allocate node
            int node = freeNode;
            freeNode = nodes[node].next;
            nodes[node].next = NULL_NODE;
            nodes[node].child1 = NULL_NODE;
            nodes[node].child2 = NULL_NODE;
            nodes[node].height = 0;
            return node;
        }

        /// <summary>
        /// Balances the sizes of bounding boxes on each node's two branches.
        /// </summary>
        /// <param name="iA">A node index to keep track of.</param>
        /// <returns>The new index of iA, if it has moved, or the same index.</returns>
        private int Balance(int iA) {
            Debug.Assert(iA != NULL_NODE);

            BVHNode A = nodes[iA];
            if (A.IsLeaf || A.height < 2) {
                return iA;
            }

            int iB = A.child1;
            int iC = A.child2;
            Debug.Assert(0 <= iB && iB < nodes.Count);
            Debug.Assert(0 <= iC && iC < nodes.Count);

            BVHNode B = nodes[iB];
            BVHNode C = nodes[iC];

            int balance = C.height - B.height;

            // Rotate C up
            if (balance > 1) {
                int iF = C.child1;
                int iG = C.child2;
                BVHNode F = nodes[iF];
                BVHNode G = nodes[iG];
                Debug.Assert(0 <= iF && iF < nodes.Count);
                Debug.Assert(0 <= iG && iG < nodes.Count);

                // Swap A and C
                C.child1 = iA;
                C.next = A.next;
                A.next = iC;

                // A's old parent should point to C
                if (C.next != NULL_NODE) {
                    if (nodes[C.next].child1 == iA) {
                        nodes[C.next].child1 = iC;
                    } else {
                        Debug.Assert(nodes[C.next].child2 == iA);
                        nodes[C.next].child2 = iC;
                    }
                } else {
                    root = iC;
                }

                // Rotate
                if (F.height > G.height) {
                    C.child2 = iF;
                    A.child2 = iG;
                    G.next = iA;
                    A.shape = BVHBox.Combine(B.shape, G.shape);
                    C.shape = BVHBox.Combine(A.shape, F.shape);

                    A.height = 1 + MathHelper.Max(B.height, G.height);
                    C.height = 1 + MathHelper.Max(A.height, F.height);
                } else {
                    C.child2 = iG;
                    A.child2 = iF;
                    F.next = iA;
                    A.shape = BVHBox.Combine(B.shape, F.shape);
                    C.shape = BVHBox.Combine(A.shape, G.shape);

                    A.height = 1 + MathHelper.Max(B.height, F.height);
                    C.height = 1 + MathHelper.Max(A.height, G.height);
                }

                return iC;
            }

            // Rotate B up
            if (balance < -1) {
                int iD = B.child1;
                int iE = B.child2;
                BVHNode D = nodes[iD];
                BVHNode E = nodes[iE];
                Debug.Assert(0 <= iD && iD < nodes.Count);
                Debug.Assert(0 <= iE && iE < nodes.Count);

                // Swap A and B
                B.child1 = iA;
                B.next = A.next;
                A.next = iB;

                // A's old parent should point to B
                if (B.next != NULL_NODE) {
                    if (nodes[B.next].child1 == iA) {
                        nodes[B.next].child1 = iB;
                    } else {
                        Debug.Assert(nodes[B.next].child2 == iA);
                        nodes[B.next].child2 = iB;
                    }
                } else {
                    root = iB;
                }

                // Rotate
                if (D.height > E.height) {
                    B.child2 = iD;
                    A.child1 = iE;
                    E.next = iA;
                    A.shape = BVHBox.Combine(C.shape, E.shape);
                    B.shape = BVHBox.Combine(A.shape, D.shape);

                    A.height = 1 + MathHelper.Max(C.height, E.height);
                    B.height = 1 + MathHelper.Max(A.height, D.height);
                } else {
                    B.child2 = iE;
                    A.child1 = iD;
                    D.next = iA;
                    A.shape = BVHBox.Combine(C.shape, D.shape);
                    B.shape = BVHBox.Combine(A.shape, E.shape);

                    A.height = 1 + MathHelper.Max(C.height, D.height);
                    B.height = 1 + MathHelper.Max(A.height, E.height);
                }

                return iB;
            }

            return iA;
        }

    }

}

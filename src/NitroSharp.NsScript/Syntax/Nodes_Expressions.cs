﻿using System;
using System.Collections.Immutable;

namespace NitroSharp.NsScript.Syntax
{
    public abstract class ExpressionSyntax : SyntaxNode
    {
        protected ExpressionSyntax(TextSpan span) : base(span)
        {
        }
    }

    public sealed class LiteralExpressionSyntax : ExpressionSyntax
    {
        internal LiteralExpressionSyntax(in ConstantValue value, TextSpan span) : base(span)
        {
            Value = value;
        }

        public ConstantValue Value { get; }
        public override SyntaxNodeKind Kind => SyntaxNodeKind.LiteralExpression;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitLiteral(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitLiteral(this);
        }
    }

    public sealed class NameExpressionSyntax : ExpressionSyntax
    {
        internal NameExpressionSyntax(string name, SigilKind sigil, TextSpan span) : base(span)
        {
            Name = name;
            Sigil = sigil;
        }

        public string Name { get; }
        public SigilKind Sigil { get; }
        public override SyntaxNodeKind Kind => SyntaxNodeKind.NameExpression;

        public override void Accept(SyntaxVisitor visitor)
        {
            //visitor.VisitIdentifier(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            throw new NotImplementedException();
            //return visitor.VisitIdentifier(this);
        }
    }

    public sealed class UnaryExpressionSyntax : ExpressionSyntax
    {
        internal UnaryExpressionSyntax(
            ExpressionSyntax operand,
            Spanned<UnaryOperatorKind> operatorKind,
            TextSpan span) : base(span)
        {
            Operand = operand;
            OperatorKind = operatorKind;
        }

        public ExpressionSyntax Operand { get; }
        public Spanned<UnaryOperatorKind> OperatorKind { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.UnaryExpression;

        public override SyntaxNode? GetNodeSlot(int index)
        {
            switch (index)
            {
                case 0: return Operand;
                default: return null;
            }
        }

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitUnaryExpression(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitUnaryExpression(this);
        }
    }

    public sealed class BinaryExpressionSyntax : ExpressionSyntax
    {
        internal BinaryExpressionSyntax(
            ExpressionSyntax left,
            Spanned<BinaryOperatorKind> operatorKind,
            ExpressionSyntax right,
            TextSpan span) : base(span)
        {
            Left = left;
            OperatorKind = operatorKind;
            Right = right;
        }

        public ExpressionSyntax Left { get; }
        public Spanned<BinaryOperatorKind> OperatorKind { get; }
        public ExpressionSyntax Right { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.BinaryExpression;

        public override SyntaxNode? GetNodeSlot(int index)
        {
            switch (index)
            {
                case 0: return Left;
                case 1: return Right;
                default: return null;
            }
        }

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitBinaryExpression(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitBinaryExpression(this);
        }
    }

    public sealed class AssignmentExpressionSyntax : ExpressionSyntax
    {
        internal AssignmentExpressionSyntax(
            ExpressionSyntax target,
            Spanned<AssignmentOperatorKind> operatorKind,
            ExpressionSyntax value,
            TextSpan span) : base(span)
        {
            Target = target;
            OperatorKind = operatorKind;
            Value = value;
        }

        public ExpressionSyntax Target { get; }
        public Spanned<AssignmentOperatorKind> OperatorKind { get; }
        public ExpressionSyntax Value { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.AssignmentExpression;

        public override SyntaxNode? GetNodeSlot(int index)
        {
            switch (index)
            {
                case 0: return Target;
                case 1: return Value;
                default: return null;
            }
        }

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitAssignmentExpression(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitAssignmentExpression(this);
        }
    }

    public sealed class FunctionCallExpressionSyntax : ExpressionSyntax
    {
        internal FunctionCallExpressionSyntax(
            Spanned<string> targetName,
            ImmutableArray<ExpressionSyntax> arguments,
            TextSpan span) : base(span)
        {
            TargetName = targetName;
            Arguments = arguments;
        }

        public Spanned<string> TargetName { get; }
        public ImmutableArray<ExpressionSyntax> Arguments { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.FunctionCallExpression;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitFunctionCall(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitFunctionCall(this);
        }
    }

    public sealed class BezierExpressionSyntax : ExpressionSyntax
    {
        public BezierExpressionSyntax(
            ImmutableArray<BezierControlPointSyntax> controlPoints,
            TextSpan span) : base(span)
        {
            ControlPoints = controlPoints;
        }

        public ImmutableArray<BezierControlPointSyntax> ControlPoints { get; }
        public override SyntaxNodeKind Kind => SyntaxNodeKind.BezierExpression;

        public override void Accept(SyntaxVisitor visitor)
        {
            throw new NotImplementedException();
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            throw new NotImplementedException();
        }
    }

    public readonly struct BezierControlPointSyntax
    {
        public readonly ExpressionSyntax X;
        public readonly ExpressionSyntax Y;
        public readonly bool IsStartingPoint;

        public BezierControlPointSyntax(ExpressionSyntax x, ExpressionSyntax y, bool starting)
            => (X, Y, IsStartingPoint) = (x, y, starting);

        public void Deconstruct(out ExpressionSyntax x, out ExpressionSyntax y)
        {
            x = X;
            y = Y;
        }
    }
}

// This file is based heavily on David Holroyd's AS3 Grammar from METAAS
//
// This file is part of flyparse-mode
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

//	@authors Martin Schnabel, David Holroyd, Aemon Cannon
// 
//  As modified Jeff Bakst
//  
grammar AS3T;

options {
	output=template;
	rewrite=true;
	language=CSharp3;	
}

tokens {
    NEW_EXPRESSION;IF_STMT;ELSE_CLAUSE;NAME;QUALIFIED_NAME;ASSIGNMENT_EXPR;
    PROP_ACCESS; ARRAY_ACCESS; ARRAY_SUBSCRIPT; E4X_EXPRESSION;PRIMARY_EXPRESSION;CLASS_NAME;
    EXPRESSION;ADDITIVE_EXP;EXTENDS_CLAUSE;IMPLEMENTS_CLAUSE;INCLUDE_DIRECTIVE;METHOD_NAME;
	PACKAGE_DECL;IMPORT_DEF;DECLARATION;VAR_DECLARATION;VAR_INITIALIZER;RETURN_STATEMENT;
	CONDITION;ACCESSOR_ROLE;SWITCH_STATEMENT_LIST;FOR_IN_LOOP;FOR_EACH_LOOP;VARIABLE_DEF;
	IDENTIFIER_STAR;ANNOTATION;ANNOTATIONS;ANNOTATION_PARAMS;ARGUMENTS;ARGUMENT;PRE_DEC;PRE_INC;
	PROP_OR_IDENT;
	COMPILATION_UNIT;
	PACKAGE;
	IMPORT;
	METADATA;
	METADATA_ITEM;
	CLASS_DEF; INTERFACE_DEF;
	EXTENDS_CLAUSE; IMPLEMENTS_CLAUSE; TYPE_BLOCK;METHOD_BLOCK;
	MODIFIERS; VARIABLE_DEF; METHOD_DEF; NAMESPACE_DEF; PARAMS; PARAM; TYPE_SPEC;
	BLOCK; EXPR; ELIST; EXPR_STMNT;EXPR_LIST;
	NEW_EXPR; ENCPS_EXPR;
	VAR_INIT;
	FUNCTION_CALL; ARRAY_ACC;
	UNARY_PLUS; UNARY_MINUS; POST_INC; POST_DEC;
	ARRAY_LITERAL; ELEMENT; OBJECT_LITERAL; OBJECT_FIELD; FUNC_DEF;
	FOR_INIT; FOR_CONDITION; FOR_ITERATOR;
	CLASS;INTERFACE;EXTENDS;IMPLEMENTS;
	METHOD;NAMESPACE;FOR_IN_CLAUSE;FOR_CLAUSE;FOR_LOOP;
	CASE;CASE_DEFAULT;SWITCH_BLOCK;SWITCH;BREAK;TRY_STATEMENT;THROW_STATEMENT;
	CONTINUE;CONTINUE_STATEMENT;BREAK_STATEMENT;SWITCH_STATEMENT;
	RETURN;BREAK;IF;ELSE;THROW;
	STATEMENT;WHILE;DO_WHILE;WHILE;WHILE_LOOP;DO_WHILE_LOOP;
	STATEMENT_BLOCK;PARAM_DECL;PARAM_REST_DECL;VAR_DEC;VARIABLE_DECLARATOR;
	PARAM_LIST;WITH;IDENTIFIER;DECL_STMT;
	MODIFIER_LIST;CLASS_MEMBER;
	REGEX; 
	XML; 
	NAMESPACE_USAGE;
	CONSTANT;LITERAL_NUMBER;LITERAL_STRING;LITERAL_DOUBLE_STRING;LITERAL_SINGLE_STRING;
	LITERAL_REGEX;LITERAL_XML;
	DEFAULT_XML_NAMESPACE;
    CONSTANT;
}

@parser::header {
using System;
using System.IO;
using com.redwine.xas;
using Antlr.Runtime.Tree;
}

@lexer::members {
            private IToken lastToken;

            public IToken nextToken() {
                CommonToken t = (CommonToken)base.NextToken();
                if(t.Channel != Hidden){
                    lastToken = t;
                }
                return t;
            }

            private bool constantIsOk() {
                int type = (null != lastToken) ? lastToken.Type : 0;
                return type == ASSIGN || type == LPAREN || type == LBRACK || 
                    type == RETURN || type == COLON || type == LNOT || type == LT || 
                    type == GT || type == EQUAL || type == COMMA;
            }
}


@parser::members {
            private AS3TLexer lexer;
            private ICharStream cs;
			private string _classname;
			private string _basetype;
			private int _modifier;
			private int _scope;
			private int _storage;
			private int _accessor;
			private string _namespace;
			private bool _infunc = false;

			private SymbolTable _symtab = new SymbolTable();

            public void setInput(AS3TLexer lexer, ICharStream cs) {
                this.lexer = lexer;
                this.cs = cs;
            }

            // Used in tree rewrite rules to insert semicolon tree IF it exists..
            private CommonTree maybeSemi(AstParserRuleReturnScope<CommonTree, IToken> semi){
                return (semi.Start.Type == SEMI ? (CommonTree)semi.Tree : null);
            }

        private string _outputpath;

        public string OutputPath
        {
            set
            {
                _outputpath = value;
            }
            get
            {
                return _outputpath;
            }
        }

        public string Classname
        {
            set
            {
                _classname = value;
            }
            get
            {
                return _classname;
            }
        }

        public string Basetype
        {
            set
            {
                _basetype = value;
            }
            get
            {
                return _basetype;
            }
        }

        public SymbolTable SymTable
        {
            set
            {
                _symtab = value;
            }
            get
            {
                return _symtab;
            }
        }

		public void gen(StreamWriter sw)
		{
			Console.WriteLine("parser gen()");

            sw.WriteLine("\n// instance variables");

            sw.WriteLine("public:\n");
            foreach (Symbol s in _symtab)
            {
                switch (s.Kind)
                {
                    case Symbol.VARIABLE:
                        if (s.Scope == Symbol.PUBLIC)
                        {
							string storage = "";
							switch (s.Storage)
							{
								case Symbol.STATIC:
									storage = "static";
									break;
							}
                            sw.WriteLine("\t{0} {1} {2};", storage, s.ReturnType, s.Name);
                        }
                        break;
                    default:
                        break;
                }
            }

            sw.WriteLine("protected:\n");
            foreach (Symbol s in _symtab)
            {
                switch (s.Kind)
                {
                    case Symbol.VARIABLE:
                        if (s.Scope == Symbol.PROTECTED && s.Storage != Symbol.STATIC)
                        {
                            sw.WriteLine("\t{0} {1};", s.ReturnType, s.Name);
                        }
                        break;
                    default:
                        break;
                }
            }

            sw.WriteLine("private:\n");
            foreach (Symbol s in _symtab)
            {
                switch (s.Kind)
                {
                    case Symbol.VARIABLE:
                        if (s.Scope == Symbol.PRIVATE && s.Storage != Symbol.STATIC)
                        {
                            sw.WriteLine("\t{0} {1};", s.ReturnType, s.Name);
                        }
                        break;
                    default:
                        break;
                }
            }

            sw.WriteLine("\n// methods");
            sw.WriteLine("public:\n");

            foreach (Symbol s in _symtab)
            {
                switch (s.Kind)
                {
                    case Symbol.FUNCTION:
						if (s.Scope == Symbol.PUBLIC)
						{
							string prefix = "";
							switch (s.Accessor)
							{
								case Symbol.SETTER:
								prefix = "set_";
								break;
								case Symbol.GETTER:
								prefix = "get_";
								break;
							}
							if (s.Name == Classname)
							{
								sw.WriteLine("\t{0} {1}{2}{3};", s.ReturnType, prefix, s.Name, s.ArgList);
								sw.WriteLine("\tvirtual ~{0}();", Classname);
							}
							else
							{
								sw.WriteLine("\t{0} {1}{2}{3};", s.ReturnType, prefix, s.Name, s.ArgList);
							}
						}
						break;
                    default:
                        break;
                }
            }

            sw.WriteLine("\n// methods");
            sw.WriteLine("protected:\n");
            foreach (Symbol s in _symtab)
            {
                switch (s.Kind)
                {
                    case Symbol.FUNCTION:
						if (s.Scope == Symbol.PROTECTED)
						{
							string prefix = "";
							switch (s.Accessor)
							{
								case Symbol.SETTER:
								prefix = "set_";
								break;
								case Symbol.GETTER:
								prefix = "get_";
								break;
							}
							sw.WriteLine("\t{0} {1}{2}{3};", s.ReturnType, prefix, s.Name, s.ArgList);
						}
						break;
                    default:
                        break;
                }
            }

            sw.WriteLine("\n// methods");
            sw.WriteLine("private:\n");
            foreach (Symbol s in _symtab)
            {
                switch (s.Kind)
                {
                    case Symbol.FUNCTION:
						if (s.Scope == Symbol.PRIVATE)
						{
							string prefix = "";
							switch (s.Accessor)
							{
								case Symbol.SETTER:
								prefix = "set_";
								break;
								case Symbol.GETTER:
								prefix = "get_";
								break;
							}
							sw.WriteLine("\t{0} {1}{2}{3};", s.ReturnType, prefix, s.Name, s.ArgList);
						}
						break;
                    default:
                        break;
                }
            }

			Console.WriteLine("parser gen()--");
		}

}


/**
 * this is the start rule for this parser
 */
public program : compilationUnit
	;

compilationUnit
	:	( as2CompilationUnit | as3CompilationUnit )
	;

as2CompilationUnit
@after {$st = %{$text};}
	:	importDefinition*
		as2Type
	;

as2Type
	:	
	(	as2IncludeDirective
	|	(modifiers CLASS) => as2ClassDefinition
	|	(modifiers INTERFACE) => as2InterfaceDefinition
	)
	;

as3CompilationUnit
@after {$st = %{$text};}
	:	packageDecl
		packageBlockEntry*
		EOF
	;

packageDecl
@after {$st = %{$text};}
	:	PACKAGE id=identifierStar? {_namespace = $identifierStar.text;}
		LCURLY	
        packageBlockEntry*
		RCURLY -> file1(x={$text},defs={$packageBlockEntry.text})
		
	;

 packageBlockEntry options {k=2;}
 @after {$st = %{$text};}
	:	    importDefinition
		|   includeDirective
		|   useNamespaceDirective
		|   (LBRACK IDENT) => annotation
		|   (modifiers NAMESPACE) => namespaceDefinition
        |   (modifiers CLASS) => classDefinition
		|   (modifiers INTERFACE) => interfaceDefinition
		|   (modifiers FUNCTION) => methodDefinition
		|   (modifiers varOrConst) => variableDefinition
        |   statement
	;

endOfFile
@after {$st = %{$text};}
	:	EOF
	;

importDefinition
@after {$st = %{$text};}
	:	IMPORT identifierStar s=semi -> include(name={$identifierStar.text})
	        
	;

semi 
	: SEMI
	; 

classDefinition
@after {$st = %{$text};}
	:	modifiers
		CLASS 
        ident {_classname = $ident.text; }
		classExtendsClause?
		implementsClause?
		typeBlock -> comment(s={$text})
		
	;

as2ClassDefinition
@after {$st = %{$text};}
	:	modifiers
		CLASS identifier
		classExtendsClause?
		implementsClause?
		typeBlock
		
	;

interfaceDefinition
@after {$st = %{$text};}
	:	modifiers
		INTERFACE ident
		interfaceExtendsClause?
		typeBlock
		
	;

as2InterfaceDefinition
@after {$st = %{$text};}
	:	modifiers
		INTERFACE identifier
		interfaceExtendsClause?
		typeBlock
		
	;

classExtendsClause
@after {$st = %{$text};}
	:	EXTENDS identifier {_basetype = $identifier.text; }
        
	;

interfaceExtendsClause
@after {$st = %{$text};}
	:	EXTENDS identifier ( COMMA identifier)*
        
	;

implementsClause
@after {$st = %{$text};}
	:	IMPLEMENTS i1=identifier ( COMMA i2=identifier)* -> interfaces(i={$i1.text})
        
	;

typeBlock
@after {$st = %{$text};}
	:	LCURLY
        typeBlockEntry*
		RCURLY
		
	;

typeBlockEntry options { k=2; }
@after {$st = %{$text};}
	: 
      includeDirective
	| importDefinition
	| (LBRACK IDENT) => annotation
	| (modifiers varOrConst) =>  variableDefinition 
	| (modifiers FUNCTION) => methodDefinition 
	| statement
	;

as2IncludeDirective
	:	INCLUDE_DIRECTIVE
		stringLiteral
	;

includeDirective
	:	'include' stringLiteral s=semi
        
	;


methodDefinition
@init { _infunc = true; }
@after {_infunc = false;}
	:
		modifiers
		FUNCTION
        accessorRole?
		methodName
		parameterDeclarationList
		typeExpression?
        maybeBlock {
			Symbol s = new Symbol();
			s.Name = $methodName.text;
			s.EnclosingType = _classname;
			s.Kind = Symbol.FUNCTION;
			s.ArgList = $parameterDeclarationList.text;
			s.Scope = _scope;
			s.Accessor = _accessor;
			s.ReturnType = $typeExpression.text;

			_symtab.Add(s);
			_accessor = -1;
			_scope = -1;
		}
		-> method(class={_classname}, name={$methodName.text}, ret={$typeExpression.text}, args={$parameterDeclarationList.text}, block={$maybeBlock.text}, accessor={$accessorRole.text})
		
	;

maybeBlock options {k=1;}
    : 
    (LCURLY) => block
    |   
    ;

methodName
@after {$st = %{$text};}
    : ident
    ;


accessorRole
@after {$st = %{$text};}
	: GET {_accessor = Symbol.GETTER; }
	| SET {_accessor = Symbol.SETTER; }
	;

namespaceDefinition
	:	modifiers NAMESPACE namespaceName
		
	;

useNamespaceDirective
	:	USE NAMESPACE namespaceName s=semi
	;

variableDefinition
@after {$st = %{$text};}
	:	modifiers
		varOrConst v1=variableDeclarator
		(COMMA v2=variableDeclarator)*
		s=semi
		 -> comment(s={$text})
	;

declaration
@after {$st = %{$text};}
	:	varOrConst variableDeclarator declarationTail -> decl2(name={$variableDeclarator.text})
        
	;

varOrConst
@after {$st = %{$text};}
	: VAR -> ignore()
	| CONST { _modifier = Symbol.CONST; } -> ignore()
	;

declarationTail
	:	(COMMA variableDeclarator)*
	;

variableInitializer
@after {$st = %{$text};}
	:	ASSIGN expression -> assign(rhs={$expression.text})
        
	;

variableDeclarator
@after {$st = %{$text};}
	:	id=ident typeExpression? variableInitializer?
			{
			if (!_infunc) 
			{
				Symbol sym = new Symbol();
				sym.Name = $ident.st.ToString();
				sym.Kind = Symbol.VARIABLE;
				sym.ReturnType = $typeExpression.st.ToString();
				sym.Scope = _scope;
				sym.Modifier = _modifier;
				sym.Storage = _storage;

				_symtab.Add(sym);
			}

			_storage = -1;
			_scope = -1;
			_modifier = -1;
		}
	-> var(name={$ident.st}, type={$typeExpression.st}, init={$variableInitializer.st}, ptr={""})
	;


// A list of formal parameters
// TODO: shouldn't the 'rest' parameter only be allowed in the last position?
parameterDeclarationList
	:	LPAREN
		(	parameterDeclaration
			(COMMA parameterDeclaration)*
		)?
		RPAREN
		
	;


parameterDeclaration
@after {$st = %{$text};}
	:	basicParameterDeclaration | parameterRestDeclaration
	;

basicParameterDeclaration
@after {$st = %{$text};}
	:	CONST? ident typeExpression? parameterDefault? -> paramdecl(name={$ident.text}, type={$typeExpression.text}, init={$parameterDefault.text})
		
	;

parameterDefault
@after {$st = %{$text};}
		// TODO: can we be more strict about allowed values?
	:	ASSIGN assignmentExpression
	;

parameterRestDeclaration
@after {$st = %{$text};}
	:	REST ident? typeExpression? -> param(name={$ident.text}, type={$typeExpression.text})
		
	;

block
@after {$st = %{$text};}
	:	LCURLY blockEntry* RCURLY
		
	;

blockEntry
	: statement
	;

condition
	:	LPAREN expression RPAREN
		
	;

statement
@after {$st = %{$text};}
	: (LCURLY) => block
	| declarationStatement 
	| expressionStatement
	| ifStatement
	| forStatement
//	| forEachStatement
	| whileStatement
	| doWhileStatement
	| withStatement
	| switchStatement
	| breakStatement
	| continueStatement
	| returnStatement
	| throwStatement
	| tryStatement
	| defaultXMLNamespaceStatement
    | semi
	;

declarationStatement
@after {$st = %{$text};}
	:	declaration s=semi
      
	;

expressionStatement
@after {$st = %{$text};}
	: expressionList s=semi
		
	;

ifStatement
@after {$st = %{$text};}
	:	IF condition statement
		((ELSE)=>elseClause)?
        
	;

elseClause
@after {$st = %{$text};}
	:	ELSE statement
        
	;
	
throwStatement
@after {$st = %{$text};}
	:	'throw' expression s=semi
        
	;

tryStatement
	:	'try'
		block
		catchBlock*
		finallyBlock?
        
	;

catchBlock
	:	'catch' LPAREN ident typeExpression? RPAREN
		block  -> catchBlock(t={$typeExpression.text},id={$ident.text},b={$block.text})
	;

finallyBlock
	:	'finally' block
	;

returnStatement
@after {$st = %{$text};}
	:	RETURN expression? s=semi
        
	;
		
continueStatement
@after {$st = %{$text};}
	:	CONTINUE s=semi
        
	;

breakStatement
	:	BREAK s=semi
        
	;

switchStatement
	:	SWITCH condition
		switchBlock
        
	;

switchBlock
	:	LCURLY
		(caseStatement)*
		(defaultStatement)?
		RCURLY
		
	;

caseStatement
	:	CASE expression COLON l=switchStatementList 
	;
	
defaultStatement
@after {$st = %{$text};}
	:	DEFAULT COLON l=switchStatementList 
	;

switchStatementList
	:	statement* 
	;

forEachStatement
	:	f=FOR EACH
		LPAREN
		forInClause
		RPAREN
		statement
		
	;

forStatement
@after {$st = %{$text};}
	:	f=FOR
		LPAREN
		( forInClause RPAREN statement | traditionalForClause RPAREN statement)
	;

traditionalForClause
	:	i=forInit  semi 	// initializer
		c=forCond  semi	// condition test
		u=forIter // updater
	;

forInClause
	:	forInClauseDecl IN forInClauseTail
	;

forInClauseDecl
	: varOrConst ident typeExpression? 
    | ident
	;


forInClauseTail
	:	expressionList
	;

// The initializer for a for loop
forInit
@after {$st = %{$text};}
	:	(declaration | expressionList )?
	;

forCond
@after {$st = %{$text};}
	:	expressionList?
	;

forIter
@after {$st = %{$text};}
	:	expressionList?
	;

whileStatement
	:	WHILE condition statement
		
	;

doWhileStatement
	:	DO statement WHILE condition semi
		
	;

withStatement
	:	WITH condition statement
	;

defaultXMLNamespaceStatement
	:	DEFAULT XML NAMESPACE ASSIGN expression semi
		
	;

typeExpression
@after {$st =  %{$text};}
	: COLON typeIdentifier -> type(t={$typeIdentifier.text})
	| 'void'  -> type(t={"void"})
	| STAR -> type(t={"void*"})
    ;

typeIdentifier
@after {$st = %{$text};}
    : ident (propOrIdent)*
    ;

identifier 
@after {$st = %{$text};}
	:	(qualifiedIdent) (propOrIdent)*
	;

qualifiedIdent 
options {k=1;}
@after {$st = %{$text};}
    : (namespaceName DBL_COLON) => namespaceName DBL_COLON ident
    | ident
    ;

namespaceName
@after {$st = %{$text};}
	:	IDENT | reservedNamespace
	;

reservedNamespace
@after {$st = %{$text};}
	:	PUBLIC { _scope = Symbol.PUBLIC; } -> ignore()
	|	PRIVATE { _scope = Symbol.PRIVATE; } -> ignore()
	|	PROTECTED { _scope = Symbol.PROTECTED; } -> ignore()
	|	INTERNAL { _scope = Symbol.INTERNAL; } -> ignore()
	;

identifierStar
@after {$st = %{$text};}
	:	ident
		dotIdent*
		(DOT STAR)?
	;

dotIdent
    : DOT ident -> path(p={$ident.text})
    ;

ident
@after {$st = %{$text};}
	:	IDENT 
	|	USE
	|	XML
	| TRACE
	|	DYNAMIC
	|	NAMESPACE
	|	IS
	|	AS
	|	GET
	|	SET
	|	z=SUPER -> comment(s={$z.text})
	;

annotation
@after {$st = %{$text};}
	:	LBRACK
		ident
		annotationParamList?
		RBRACK -> notsupported(s={$text})
		
	;

annotationParamList
	:
		LPAREN
		(	annotationParam
			(COMMA annotationParam)*
		)?
		RPAREN
		
	;

annotationParam
	: ident ASSIGN constant 
	| constant 
	| ident 
	;

modifiers
@after {$st = %{$text};}
	: ( modifier (modifier)* )?
	;

modifier
@after {$st = %{$text};}
	:	namespaceName
	|	STATIC { _storage = Symbol.STATIC; } -> ignore()
	|	'final'
	|	'enumerable'
	|	'explicit'
	|	'override'
	|	DYNAMIC
	|	'intrinsic'
	;

arguments
@after {$st = %{$text};}
	:	LPAREN expressionList RPAREN
	|	LPAREN RPAREN
	;

// This is an initializer used to set up an array.
arrayLiteral
	:	LBRACK elementList? RBRACK
	;
		
elementList
	:	COMMA
	|	nonemptyElementList
	;

nonemptyElementList
	:	assignmentExpression (COMMA assignmentExpression)*
	;

element
	:	assignmentExpression
	;

// This is an initializer used to set up an object.
objectLiteral
	:	LCURLY fieldList? RCURLY
	;
	
fieldList
	:	literalField (COMMA literalField?)*
	;
	
literalField 
	: 	fieldName COLON element
	;
	
fieldName
	:	ident
	|	number
	;

// the mother of all expressions
expression
@after {$st = %{$text};}
	:	assignmentExpression
	;

// This is a list of expressions.
expressionList
@after {$st = %{$text};}
	:	e1=assignmentExpression (COMMA e2=assignmentExpression)* -> assign(rhs={$e1.text})
	;

// assignment expression (level 13)
assignmentExpression
@after {$st = %{$text};}
	:	x1=conditionalExpression ((assignmentOperator) => op1=assignmentOperator x2=assignmentExpression )* -> assignexpr(lhs={$x1.text},op={$op1.text},rhs={$x2.text})
	;

assignmentOperator
@after {$st = %{$text};}
	:	ASSIGN
	| 	STAR_ASSIGN
	|	DIV_ASSIGN
	|	MOD_ASSIGN
	|	PLUS_ASSIGN
	|	MINUS_ASSIGN
	|	SL_ASSIGN
	|	SR_ASSIGN
	|	BSR_ASSIGN
	|	BAND_ASSIGN
	|	BXOR_ASSIGN
	|	BOR_ASSIGN
	|	LAND_ASSIGN
	|	LOR_ASSIGN
	;

// conditional test (level 12)
conditionalExpression
	:	(logicalOrExpression)
		(
			QUESTION
			conditionalSubExpression
			
		)?
	;

conditionalSubExpression
	:	assignmentExpression COLON assignmentExpression
	;

// TODO: should 'and'/'or' have same precidence as '&&'/'||' ?

// logical or (||)  (level 11)
logicalOrExpression
	:	logicalAndExpression
		(logicalOrOperator logicalAndExpression)*
	;

logicalOrOperator
	:	LOR | 'or'
	;

// logical and (&&)  (level 10)
logicalAndExpression
	:	bitwiseOrExpression
		(logicalAndOperator bitwiseOrExpression)*
	;

logicalAndOperator
	:	LAND | 'and'
	;

// bitwise or non-short-circuiting or (|)  (level 9)
bitwiseOrExpression
	:	bitwiseXorExpression
		(BOR bitwiseXorExpression)*
	;

// exclusive or (^)  (level 8)
bitwiseXorExpression
	:	bitwiseAndExpression
		(BXOR bitwiseAndExpression)*
	;

// bitwise or non-short-circuiting and (&)  (level 7)
bitwiseAndExpression
	:	equalityExpression
		(BAND equalityExpression)*
	;

// equality/inequality (==/!=) (level 6)
equalityExpression
	:	relationalExpression
        (equalityOperator relationalExpression)*
	;

equalityOperator
	:	STRICT_EQUAL | STRICT_NOT_EQUAL | NOT_EQUAL | EQUAL
	;
	
// boolean relational expressions (level 5)
relationalExpression
	:	s1=shiftExpression (relationalOperator s2=shiftExpression)* -> expr(e={$s1.text},r={$relationalOperator.text},c={$s2.text})
	;

relationalOperator
@after {$st = %{$text};}
	: LT -> relationalOperator(op={"<"})
	| GT  -> relationalOperator(op={">"})
	| LE  -> relationalOperator(op={"<="})
	| GE  -> relationalOperator(op={">="})
	| IS -> rtti()
	| AS -> cast()
	| 'instanceof'
	;

// bit shift expressions (level 4)
shiftExpression
@after {$st = %{$text};}
	:	additiveExpression
		(shiftOperator additiveExpression)*
	;

shiftOperator
@after {$st = %{$text};}
	:	SL | SR | BSR
	;

// binary addition/subtraction (level 3)
additiveExpression
@after {$st = %{$text};}
	:	multiplicativeExpression (additiveOperator multiplicativeExpression)*
	;

additiveOperator
@after {$st = %{$text};}
	:	PLUS | MINUS
	;

// multiplication/division/modulo (level 2)
multiplicativeExpression
@after {$st = %{$text};}
	:	unaryExpression
		(	multiplicativeOperator
			unaryExpression
		)*
	;

multiplicativeOperator
@after {$st = %{$text};}
	:	STAR | DIV | MOD
	;

//	(level 1)
unaryExpression
@after {$st = %{$text};}
	:	inc=INC unaryExpression
	|	dec=DEC unaryExpression
	|	MINUS unaryExpression
	|	PLUS unaryExpression
	|	unaryExpressionNotPlusMinus
	;

unaryExpressionNotPlusMinus
@after {$st = %{$text};}
	:	'delete' postfixExpression 
	|	'void' unaryExpression 
	|	'typeof' unaryExpression 
	|	LNOT unaryExpression 
	|	BNOT unaryExpression 
	|	postfixExpression
	;

/* Array expressions, function invocation, post inc/dec

Note: $postfixExpression refers to the current tree for this
rule. So, in the *, we are repeatedly re-parenting the tree */

postfixExpression
	:	primaryExpression
		(	//qualified names
            propOrIdent
            
		|	//E4X expression
            DOT e4xExpression
            
		|	//Extended E4X expression
            E4X_DESC e4xExpression
            
            
		|	//array access
            LBRACK expression RBRACK
            

		|	// A method invocation
            arguments
            
		)*
        
		( 	INC 
	 	|	DEC 
		)?

 	;

e4xExpression
	:	STAR
	|	e4xAttributeIdentifier
	|	e4xFilterPredicate
	;

e4xAttributeIdentifier
	:	E4X_ATTRI
		(	qualifiedIdent
		|	STAR
		|	LBRACK expression RBRACK
		)
	;

e4xFilterPredicate
	:	LPAREN
		expression
		RPAREN
	;

primaryExpression
@after {$st = %{$text};}
	:	'undefined'
	|	constant
	|	arrayLiteral
	|	objectLiteral
	|	functionDefinition
	|	newExpression
	|	encapsulatedExpression
	|	e4xAttributeIdentifier
	|	qualifiedIdent

	;

propOrIdent
	:	
		DOT qualifiedIdent -> indirect(name={$qualifiedIdent.text})
		/* without further semantic analysis, we can't
		   tell if a.b is an access of the property 'b'
		   from the var 'a' or a reference to the type
		   'b' in the package 'a'.  (This could be
		   resolved in an AST post-processing step) */
		
	;

constant
	:	xmlLiteral -> xml(x={$xmlLiteral.text})
	|	regexpLiteral  -> regex(x={$regexpLiteral.text})
	|	number 
	|	stringLiteral 
	|	TRUE
	|	FALSE
	|	NULL -> null()
	;

stringLiteral
    : stringLiteralDouble | stringLiteralSingle
    ;

stringLiteralDouble
    : STRING_LITERAL_DOUBLE 
    ;

stringLiteralSingle
    : STRING_LITERAL_SINGLE 
    ;


number	:	HEX_LITERAL
	|	DECIMAL_LITERAL
	|	OCTAL_LITERAL
	|	FLOAT_LITERAL
	;

	
xmlLiteral
@after {$st = %{$text};}
	: XML_LITERAL
	;


regexpLiteral
	:	REGEX_LITERAL
	;

newExpression
	:	NEW fullNewSubexpression arguments 
	;

fullNewSubexpression
	:	(	primaryExpression 
		)
		(	d=DOT qualifiedIdent 
		|	brackets 
		)*
	;

propertyOperator
	:	DOT qualifiedIdent
	|	brackets
	;

brackets
	:	LBRACK expressionList RBRACK
	;

encapsulatedExpression
	:	LPAREN assignmentExpression RPAREN
		
	;

// TODO: should anonymous and named functions have seperate definitions so that
// we can dissallow named functions in expressions?

functionDefinition
	:	FUNCTION parameterDeclarationList typeExpression? block
	;

PACKAGE		:	'package';
PUBLIC		:	'public';
PRIVATE		:	'private';
PROTECTED	:	'protected';
INTERNAL	:	'internal';
FUNCTION	:	'function';
EXTENDS		:	'extends';
IMPLEMENTS	:	'implements';
VAR		:	'var';
STATIC		:	'static';
IF		:	'if';
IMPORT		:	'import';
FOR		:	'for';
EACH		:	'each';
IN		:	'in';
WHILE		:	'while';
DO		:	'do';
SWITCH		:	'switch';
CASE		:	'case';
DEFAULT		:	'default';
ELSE		:	'else';
CONST		:	'const';
CLASS		:	'class';
INTERFACE	:	'interface';
TRUE		:	'true';
FALSE		:	'false';
DYNAMIC		:	'dynamic';
USE		:	'use';
TRACE   : 'trace';
XML		:	'xml';
NAMESPACE	:	'namespace';
IS		:	'is';
AS		:	'as';
GET		:	'get';
SET		:	'set';
WITH		:	'with';
RETURN		:	'return';
CONTINUE	:	'continue';
BREAK		:	'break';
NULL		:	'null';
NEW		    :	'new';
SUPER		:	'super';

// OPERATORS
QUESTION		:	'?'	;
LPAREN			:	'('	;
RPAREN			:	')'	;
LBRACK			:	'['	;
RBRACK			:	']'	;
LCURLY			:	'{'	;
RCURLY			:	'}'	;
COLON			:	':'	;
DBL_COLON		:	'::'	;
COMMA			:	','	;
ASSIGN			:	'='	;
EQUAL			:	'=='	;
STRICT_EQUAL		:	'==='	;
LNOT			:	'!'	;
BNOT			:	'~'	;
NOT_EQUAL		:	'!='	;
STRICT_NOT_EQUAL	:	'!=='	;
PLUS			:	'+'	;
PLUS_ASSIGN		:	'+='	;
INC			:	'++'	;
MINUS			:	'-'	;
MINUS_ASSIGN		:	'-='	;
DEC			:	'--'	;
STAR			:	'*'	;
STAR_ASSIGN		:	'*='	;
MOD			:	'%'	;
MOD_ASSIGN		:	'%='	;
SR			:	'>>'	;
SR_ASSIGN		:	'>>='	;
BSR			:	'>>>'	;
BSR_ASSIGN		:	'>>>='	;
GE			:	'>='	;
GT			:	'>'	;
BXOR			:	'^'	;
BXOR_ASSIGN		:	'^='	;
BOR			:	'|'	;
BOR_ASSIGN		:	'|='	;
LOR			:	'||'	;
BAND			:	'&'	;
BAND_ASSIGN		:	'&='	;
LAND			:	'&&'	;
LAND_ASSIGN		:	'&&='	;
LOR_ASSIGN		:	'||='	;
E4X_ATTRI		:	'@'	; 
SEMI			:	';'	;
BSLASH          :   '\\';

DOT		:	'.'	;
E4X_DESC	:	'..'	;
REST		:	'...'	;

REGEX_LITERAL
	: { constantIsOk() }?=> '/' REGEX_BODY '/' REGEX_POSTFIX?
	;


fragment REGEX_POSTFIX
    : ('a'..'z'|'A'..'Z'|'_'|'0'..'9'|'$')+
    ;

fragment REGEX_BODY
	:	(	(~('\n'|'\r'|'*'|'/'|'\\'))
		|	'\\'(~('\n'|'\r'))
		)
		(	(~('\n'|'\r'|'/'|'\\'))
		|	'\\'(~('\n'|'\r'))
		)*
    ;

DIV_ASSIGN		:	'/=';

DIV	            :	'/';


XML_LITERAL
	:	(XML_LITERAL) =>
      '<' IDENT (XML_WS | XML_ATTRIBUTE)*
		(	'>' (XML_SUBTREE | XML_TEXTNODE | XML_COMMENT | XML_CDATA | XML_BINDING)* 
            '</' IDENT '>'
		|	'/>'
		)
	;


fragment XML_SUBTREE
	:	'<' IDENT (XML_WS | XML_ATTRIBUTE)*
		(	'>' (XML_SUBTREE | XML_TEXTNODE | XML_COMMENT | XML_CDATA | XML_BINDING)*
			'</' IDENT '>'
		|	'/>'
		)
	;

fragment XML_ATTRIBUTE
	:	IDENT XML_WS* ASSIGN XML_WS* (STRING_LITERAL_DOUBLE | STRING_LITERAL_SINGLE | XML_BINDING)
	;

fragment XML_BINDING
	:	'{' XML_AS3_EXPRESSION '}'
	;

// it should be parsed as an AS3 expression...
fragment XML_AS3_EXPRESSION
	:	
		 (~('{'|'}'))*
	;

fragment XML_TEXTNODE
	:	(	
			XML_WS
		|	('/' ~'>') => '/'
		|	~('<'|'{'|'/'| XML_WS)
		)
	;

fragment XML_COMMENT
	:	'<!--'
		(
			XML_WS
		|	~('-'| XML_WS)
		|	('-' ~'-') => '-'
		)*
		'-->'
	;

fragment XML_CDATA
	:	'<![CDATA['
		(  XML_WS
        |  (']' ~']') => ']'
        |  ~(']'| XML_WS)
        )*
		']]>'
	;

fragment XML_WS
    :	' '
    |	'\t'
    |	'\f'
    |	'\r'
    |	'\n'
    ;



SL			:	'<<'	;
SL_ASSIGN	:	'<<='	;
LE			:	'<='	;
LT			:	'<'	;


IDENT 
    :
        ('a'..'z'|'A'..'Z'|'_'|'$')
    	('a'..'z'|'A'..'Z'|'_'|'0'..'9'|'$')*
	;

STRING_LITERAL_DOUBLE
	:	'"' (ESC|~('"'|'\\'|'\n'|'\r'))* '"'
	;

STRING_LITERAL_SINGLE
	:	'\'' (ESC|~('\''|'\\'|'\n'|'\r'))* '\''
	;


HEX_LITERAL	:	'0' ('x'|'X') HEX_DIGIT+ ;

DECIMAL_LITERAL	:	('0' | '1'..'9' '0'..'9'*) ;

OCTAL_LITERAL	:	'0' ('0'..'7')+ ;

FLOAT_LITERAL
    :   ('0'..'9')+ '.' ('0'..'9')* EXPONENT?
    |   '.' ('0'..'9')+ EXPONENT?
	;


// whitespace -- ignored
WS	:	(
			' '
		|	'\t'
		|	'\f'
		)+
		{$channel=Hidden;}
	;
NL	
	:	(
			'\r' '\n'  	// DOS
		|	'\r'    	// Mac
		|	'\n'    	// Unix
		)
		{$channel=Hidden;}
	;
	
// skip BOM bytes
BOM	:	(	'\u00EF'  '\u00BB' '\u00BF'
		|	'\uFEFF'
		)
		{ $channel=Hidden; };

// might be better to filter this out as a preprocessing step
INCLUDE_DIRECTIVE
	:	'#include'
	;

// single-line comments
SL_COMMENT
	:	'//' (~('\n'|'\r'))* ('\n'|'\r'('\n')?)?
		{$channel=Hidden;}
	;
// multiple-line comments
ML_COMMENT
	:	'/*' ( options {greedy=false;} : . )* '*/'
		{$channel=Hidden;}
	;

fragment EXPONENT
	:	('e'|'E') ('+'|'-')? ('0'..'9')+
	;
fragment HEX_DIGIT
	:	('0'..'9'|'A'..'F'|'a'..'f')
	;

fragment OCT_DIGIT
	:	'0'..'7'
	;
	
fragment ESC
	:   CTRLCHAR_ESC
	|   UNICODE_ESC
	|   OCTAL_ESC
	;

fragment CTRLCHAR_ESC
	:	'\\' ('b'|'t'|'n'|'f'|'r'|'\"'|'\''|'\\')
	;

fragment OCTAL_ESC
	:   '\\' ('0'..'3') ('0'..'7') ('0'..'7')
	|   '\\' ('0'..'7') ('0'..'7')
	|   '\\' ('0'..'7')
	;

fragment UNICODE_ESC
	:   '\\' 'u' HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT
	;

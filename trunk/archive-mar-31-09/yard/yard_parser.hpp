// Dedicated to the public domain by Christopher Diggins
// http://www.cdiggins.com
//
// Contains definitions for sample parser management classes 
// to be used with the YARD framework

#ifndef YARD_PARSER_HPP
#define YARD_PARSER_HPP

namespace yard
{
	////////////////////////////////////////////////////////////////////////
	// A skeleton parser base class

	template<typename Token_T, typename Iter_T = const Token_T*>
    struct BasicParser
    {   
        // Public typedefs 
        typedef Iter_T Iterator;
        typedef Token_T Token; 

		// Constructor
        BasicParser() 
        { }

		// Parse function
		template<typename StartRule_T>
		bool Parse(Iterator first, Iterator last)
		{
			mBegin = first;
			mEnd = last; 
			mIter = first;

			try {
				return StartRule_T::Match(*this);
			}
			catch(...) {
				return false;
			}
		}
                
        // Input pointer functions 
        Token GetElem() { return *mIter; }  
        void GotoNext() { assert(mIter < End()); ++mIter; }  
        Iterator GetPos() { return mIter; }  
        void SetPos(Iterator pos) { mIter = pos; }  
        bool AtEnd() { return GetPos() >= End(); }  
        Iterator Begin() { return mBegin; }    
        Iterator End() { return mEnd; }  

		template<typename T>
		void LogMessage(const T& x)
		{ }

	protected:

		// Member fields
		Iterator	mBegin;
		Iterator	mEnd;
        Iterator	mIter;
    };  
	
	////////////////////////////////////////////////////////////////////////
	// Extends the basic parser with abstract syntax tree (AST) 
	// building capabilities 

	template<typename Token_T, typename Iter_T = const Token_T*>
	struct TreeBuildingParser : BasicParser<Token_T, Iter_T>
    {   
		// Constructor
        TreeBuildingParser() 
			: BasicParser<Token_T, Iter_T>(), mTree()
        { }

        // Public typedefs 
        typedef typename Iter_T Iterator;
        typedef typename Token_T Token; 
		typedef typename Ast<Iterator> Tree;
		typedef typename Tree::AbstractNode Node;

		void OutputLocation()
		{
			char line[257];
			int nLine = 1;
			Iterator pFirst = this->GetPos();
			
			Iterator pTmp = this->mBegin;
			while (pTmp < pFirst) {
				if (*pTmp++ == '\n')
					++nLine;
			}

			while (pFirst > this->mBegin && *pFirst != '\n')
				pFirst--;
			if (*pFirst == '\n') {
				++pFirst;
			}
			Iterator pLast = this->GetPos();
			while (pLast < this->mEnd && *pLast != '\n') {
				pLast++;
			}
			size_t n = pLast - pFirst;
			n = n < 255 ? n : 255;
			strncpy(line, pFirst, n);	
			line[n] = '\0';

			char marker[256];
			n = this->GetPos() - pFirst;
			n = n < 254 ? n : 254;
			for (size_t i=0; i < n; ++i)
				marker[i] = ' ';
			marker[n] = '^';
			marker[n + 1] = '\0';

			fprintf(stderr, "line number %d\n", nLine); 
			fprintf(stderr, "character number %d\n", this->GetPos() - this->mBegin); 
			fprintf(stderr, "%s\n", line); 
			fprintf(stderr, "%s\n", marker);
		}

		// Parse function
		template<typename StartRule_T>
		bool Parse(Iterator first, Iterator last)
		{
			this->mBegin = first;
			this->mEnd = last; 
			this->mIter = first;

			try {
				return StartRule_T::Match(*this);
			}
			catch(...) {
				fprintf(stderr, "parsing error occured\n");
				OutputLocation();
				return false;
			}
		}
                
		// AST functions
		Node* GetAstRoot() { return mTree.GetRoot(); }
		template<typename Rule_T>
		void CreateNode() { mTree.CreateNode<Rule_T>(*this); }  		
		void CompleteNode() { mTree.CompleteNode(*this); }
		void AbandonNode() { mTree.AbandonNode(*this); }

	protected:

		// Member fields
		Tree mTree;
    };  
 }

#endif 
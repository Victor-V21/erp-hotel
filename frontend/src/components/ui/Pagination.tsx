import React from 'react'
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react'
import { Button } from './button'

interface PaginationProps {
  currentPage: number
  totalPages: number
  totalItems: number
  pageSize: number
  onPageChange: (page: number) => void
  onPageSizeChange?: (size: number) => void
  pageSizeOptions?: number[]
}

export function Pagination({
  currentPage,
  totalPages,
  totalItems,
  pageSize,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = [10, 25, 50, 100],
}: PaginationProps) {
  if (totalItems === 0) return null

  const startItem = (currentPage - 1) * pageSize + 1
  const endItem = Math.min(currentPage * pageSize, totalItems)

  const getPageNumbers = () => {
    const pages: (number | string)[] = []
    if (totalPages <= 7) {
      for (let i = 1; i <= totalPages; i++) pages.push(i)
    } else {
      if (currentPage <= 4) {
        pages.push(1, 2, 3, 4, 5, '...', totalPages)
      } else if (currentPage >= totalPages - 3) {
        pages.push(1, '...', totalPages - 4, totalPages - 3, totalPages - 2, totalPages - 1, totalPages)
      } else {
        pages.push(1, '...', currentPage - 1, currentPage, currentPage + 1, '...', totalPages)
      }
    }
    return pages
  }

  return (
    <div className="flex flex-col sm:flex-row items-center justify-between gap-3 px-3.5 py-3 border-t border-border bg-card/60 text-xs text-muted-foreground select-none">
      <div className="flex items-center gap-2">
        <span>
          Mostrando <strong className="text-foreground font-semibold">{startItem}</strong> -{' '}
          <strong className="text-foreground font-semibold">{endItem}</strong> de{' '}
          <strong className="text-foreground font-semibold">{totalItems}</strong> registros
        </span>
        {onPageSizeChange && (
          <div className="flex items-center gap-1.5 ml-2 border-l border-border pl-3">
            <span>Por pág:</span>
            <select
              value={pageSize}
              onChange={(e) => onPageSizeChange(Number(e.target.value))}
              className="border border-input rounded px-1.5 py-0.5 bg-background text-foreground text-xs"
            >
              {pageSizeOptions.map((opt) => (
                <option key={opt} value={opt}>
                  {opt}
                </option>
              ))}
            </select>
          </div>
        )}
      </div>

      <div className="flex items-center gap-1">
        <Button
          size="sm"
          variant="outline"
          onClick={() => onPageChange(1)}
          disabled={currentPage === 1}
          className="h-7 w-7 p-0"
          title="Primera página"
        >
          <ChevronsLeft size={14} />
        </Button>
        <Button
          size="sm"
          variant="outline"
          onClick={() => onPageChange(currentPage - 1)}
          disabled={currentPage === 1}
          className="h-7 w-7 p-0"
          title="Página anterior"
        >
          <ChevronLeft size={14} />
        </Button>

        <div className="flex items-center gap-1 px-1">
          {getPageNumbers().map((page, idx) =>
            typeof page === 'number' ? (
              <Button
                key={idx}
                size="sm"
                variant={currentPage === page ? 'default' : 'outline'}
                onClick={() => onPageChange(page)}
                className={`h-7 w-7 p-0 text-xs ${
                  currentPage === page ? 'font-bold bg-[#C69C4B] hover:bg-[#b0883b] text-white' : ''
                }`}
              >
                {page}
              </Button>
            ) : (
              <span key={idx} className="px-1 text-muted-foreground font-semibold">
                {page}
              </span>
            )
          )}
        </div>

        <Button
          size="sm"
          variant="outline"
          onClick={() => onPageChange(currentPage + 1)}
          disabled={currentPage >= totalPages}
          className="h-7 w-7 p-0"
          title="Página siguiente"
        >
          <ChevronRight size={14} />
        </Button>
        <Button
          size="sm"
          variant="outline"
          onClick={() => onPageChange(totalPages)}
          disabled={currentPage >= totalPages}
          className="h-7 w-7 p-0"
          title="Última página"
        >
          <ChevronsRight size={14} />
        </Button>
      </div>
    </div>
  )
}

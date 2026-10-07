import React from 'react';

export default function Pagination({ total, pageNumber, totalPages, onPageChange }) {
  const start = Math.max(1, pageNumber - 2);
  const end = Math.min(totalPages, pageNumber + 2);
  const pages = [];
  for (let i = start; i <= end; i++) pages.push(i);

  return (
    <footer className="hidden sm:block bg-gray-100 p-4">
      <div className="flex items-center align-middle justify-center">
        <div className="flex items-center gap-2">
          <button
            onClick={() => onPageChange(pageNumber - 1)}
            disabled={pageNumber === 1}
            className={`px-3 py-1 rounded-md ${pageNumber === 1 ? 'bg-gray-200 text-gray-400' : 'bg-white shadow'}`}
          >Anterior</button>

          <div className="flex items-center gap-1">
            {pages.map((p) => (
              <button
                key={p}
                onClick={() => onPageChange(p)}
                className={`px-2 py-1 rounded ${p === pageNumber ? 'bg-gray-300 text-gray-800' : 'bg-white'}`}
              >{p}</button>
            ))}
          </div>

          <button
            onClick={() => onPageChange(pageNumber + 1)}
            disabled={pageNumber === totalPages}
            className={`px-3 py-1 rounded-md ${pageNumber === totalPages ? 'bg-gray-200 text-gray-400' : 'bg-white shadow'}`}
          >Siguiente</button>
        </div>
      </div>
    </footer>
  );
}

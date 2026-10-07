export default function Sidebar({
  show = false,
  children,
  className = ""
}) {
  return (
    <aside
      className={` 
        [grid-area:sidebar]
        absolute sm:relative
        flex flex-col
        h-full pt-10 p-4
        justify-between items-start
        bg-white
        border-gray-200
        rounded-sm shadow
        -top-1 sm:top-0
        z-20
        transform transition-transform duration-200
        ${show ? "-translate-x-3" : "-translate-x-100"}  
        sm:translate-x-0
        ${className}
      `}
    >
      {children}
    </aside>
  );
}
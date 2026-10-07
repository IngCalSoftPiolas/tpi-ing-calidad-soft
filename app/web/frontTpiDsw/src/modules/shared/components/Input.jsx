function Input({label, error='', children, ...restProps})
{
    return(
        <div className='flex w-full gap-2 items-start'>
            <div
            className='flex
                        flex-col
                        h-15
                        w-full
                        sm:h-20'>
                <label >{label}</label>
                <input className= {`input-default $ {error ? 'border-red-500' : ''}`} {...restProps}/>
                {error && <p className="text-red-500 text-xs sm:text-sm">{error}</p>}
                
            </div>
            {children}
        </div>
    )

}

export default Input
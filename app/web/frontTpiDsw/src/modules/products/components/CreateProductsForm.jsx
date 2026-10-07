import { useForm } from "react-hook-form";
import Input from "../../shared/components/Input";
import Card from "../../shared/components/Card";
import { createProduct } from "../services/products.js";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import toast from "react-hot-toast";

function CreateProductsForm() {
    const { register, handleSubmit, formState: { errors } } = useForm( { mode: 'onBlur' } );
    const [errorForm, setErrorForm] = useState('');

    const navigate = useNavigate();

const onValid = async (data) => {
        const payload = {
            ...data,
            currentUnitPrice: parseFloat(data.currentUnitPrice),
            stockQuantity: parseInt(data.stockQuantity)
        };

        console.log("Enviando payload:", payload);

        const { error } = await createProduct(payload);

        if (error) {
            console.error(error);
            const errorMessage = typeof error === 'string' 
                ? error 
                : (error.message || 'Error desconocido al crear el producto');
            
            setErrorForm(errorMessage);
            toast.error('Error al crear el producto');
        } else {
            toast.success('Producto creado con éxito');
            navigate('/admin/products');
        }
    };
    
    return (
            <Card>
                <div className="relative">
                    <form onSubmit={handleSubmit(onValid)} className='flex flex-col p-8 w-full h-screen sm:gap-4'>
                        <div className="flex-1 overflow-auto pr-2 flex flex-col gap-6 pb-28">
                            <Input
                                label="SKU"
                                {...register('sku', { required: 'El SKU es obligatorio' })}
                                error={errors.sku?.message}
                            />
                            <Input
                                label="Código único"
                                {...register('internalCode')}
                            />
                            <Input
                                label="Nombre"
                                {...register('name', { required: 'El nombre es obligatorio' })}
                                error={errors.name?.message}
                            />
                            <Input
                                label="Descripción"
                                {...register('description')}
                            />
                            <Input
                                label="Precio"
                                {...register('currentUnitPrice', { required: 'El precio es obligatorio', min: { value: 0, message: 'El precio debe ser mayor a 0' } } )}
                                error={errors.currentUnitPrice?.message}
                            />
                            <Input
                                label="Stock"
                                {...register('stockQuantity', { required: 'El stock es obligatorio', min: { value: 0, message: 'El stock no debe ser negativo' } } )}
                                error={errors.stockQuantity?.message}
                            />

                            {errorForm && <p className='text-red-500 text-sm mt-3'>{errorForm}</p>}
                        </div>
                        <div className="sticky bottom-0 bg-white/80 pt-4 px-8 pb-6 z-10">
                            <button type="submit" className="w-full btn-primary">
                                Crear Producto
                            </button>
                        </div>
                    </form>

                    <button
                        type="button"
                        onClick={() => navigate('/admin/products')}
                        className="absolute top-3 right-3 flex items-center gap-2 text-sm text-gray-700 hover:underline z-20 bg-white/80 px-2 py-1 rounded"
                    >
                        <img
                            src="https://www.svgrepo.com/show/496822/back-square.svg"
                            alt="Volver"
                            className="w-5 h-5"
                        />
                        Volver
                    </button>
                </div>
            </Card>
    );
}

export default CreateProductsForm;
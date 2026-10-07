import { useForm } from "react-hook-form";
import Input from "../../shared/components/Input.jsx";
import { registerService } from "../services/register.js"; // <--- Importamos el nuevo servicio
import { useNavigate } from "react-router-dom";
import toast from 'react-hot-toast';

function RegisterForm() {
    const navigate = useNavigate();
    const {
          register,
          handleSubmit,
          watch,
          formState: {errors} 
        } = useForm({mode: 'onChange'});
           
    const passwordValue = watch("password");

    const onSubmit = async (data) => {
        // Llamada al backend
        const { error } = await registerService(
            data.username, 
            data.email, 
            data.password, 
            data.role,
            data.name,
            data.phone
        );

        if (error) {
            console.error(error);
            // Manejo de errores del backend (a veces vienen como array, a veces string)
            const msg = Array.isArray(error) 
                ? error.map(e => e.description).join(', ') 
                : (typeof error === 'string' ? error : 'Error al registrar usuario');
            
            toast.error(msg);
            return;
        }

        toast.success("Usuario registrado con éxito");
        navigate('/login');
    };
    

        return (
            <form className="flex
            flex-col
            gap-4
            sm:gap-1
            p-8
            text-sm
            sm:text-sm
            w-full
            max-w-sm
            mx-auto
            bg-white
            rounded-lg
            shadow-lg
            "
            onSubmit={handleSubmit(onSubmit)}>
                
                <Input label='Usuario'
                {...register('username',{
                    required: 'Usuario obligatorio'
                })}
                error={errors.username?.message}
                />

                <Input label='Nombre'
                {...register('name',{
                    required: 'Nombre obligatorio'
                })}
                error={errors.name?.message}
                />

                <Input label='Numero de telefono'
                {...register('phone',{
                    required: 'Número de teléfono obligatorio'
                })}
                error={errors.phone?.message}
                />

                <Input label='Email'
                {...register('email',{
                    required: 'Email obligatorio'
                })}
                error={errors.email?.message}/>

                <div className="
                    flex
                    flex-col
                    h-15
                    sm:h-20">
                <label>Rol</label>
                <select {...register('role',
                    {required : 'Debe especificar un rol'}
                )} className={`input-default ${errors.role ? 'border-red-500' : ''}`}>
                    <option value=""> Seleccione un rol...</option>
                    <option value="Admin">Administrador</option>
                    <option value="Client">Cliente</option>
                </select>
                {errors.role?.message && <p className="text-red-500 text-xs sm:text-sm">{errors.role?.message}</p>}
                </div>

                <Input
                label='Contraseña'
                type ='password'
                {...register('password',{
                    minLength:{
                        value: 8,
                        message: 'La contraseña debe tener al menos 8 caracteres'
                    }, required: 'Este campo es obligatorio'
                })}
                error={errors.password?.message}
                />

                <Input
                label='Confirmar contraseña'
                type='password'
                {...register('confirmPassword',{
                    validate: value =>
                        value === passwordValue || 'Las contraseñas no coinciden'
                })}
                error={errors.confirmPassword?.message}
                />

                <button type="submit">Registrar Usuario</button>
            </form>

        );


}

export default RegisterForm;